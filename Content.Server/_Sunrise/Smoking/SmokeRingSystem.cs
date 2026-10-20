using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Buckle.Components;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Smoking;

/// <summary>
/// Система обработки полета, столкновений и распада колечка дыма на пар.
/// </summary>
public sealed partial class SmokeRingSystem : EntitySystem
{
    [ValidatePrototypeId<EntityPrototype>]
    public static readonly EntProtoId SmokeRingPrototype = "EffectSmokeRing";

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmokeRingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SmokeRingComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<SmokeRingComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<SmokeRingComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<SmokeRingComponent> entity, ref ComponentInit args)
    {
        entity.Comp.SpawnTime = _timing.CurTime;
    }

    private void OnCollide(Entity<SmokeRingComponent> entity, ref StartCollideEvent args)
    {
        if (entity.Comp.Dissipated)
            return;

        if (args.OtherEntity == entity.Comp.Shooter)
            return;

        if (args.OtherEntity == entity.Comp.IgnoredSeat)
            return;

        if (entity.Comp.Shooter != null &&
            TryComp<BuckleComponent>(entity.Comp.Shooter, out var buckle) &&
            buckle.BuckledTo == args.OtherEntity)
        {
            return;
        }

        // В первые 0.15 секунды полета игнорируем препятствия, находящиеся ближе 0.65 м от стрелка
        if (_timing.CurTime - entity.Comp.SpawnTime < TimeSpan.FromSeconds(0.15f))
        {
            if (entity.Comp.Shooter != null)
            {
                var shooterPos = Transform(entity.Comp.Shooter.Value).Coordinates;
                var otherPos = Transform(args.OtherEntity).Coordinates;
                if (shooterPos.TryDistance(EntityManager, otherPos, out var dist) && dist < 0.65f)
                    return;
            }
        }

        if (!args.OtherFixture.Hard)
            return;

        DissipateRing(entity);
    }

    private void OnThrowHit(Entity<SmokeRingComponent> entity, ref ThrowDoHitEvent args)
    {
        if (entity.Comp.Dissipated)
            return;

        if (args.Target == entity.Comp.Shooter || args.Target == entity.Comp.IgnoredSeat)
            return;

        if (entity.Comp.Shooter != null &&
            TryComp<BuckleComponent>(entity.Comp.Shooter, out var buckle) &&
            buckle.BuckledTo == args.Target)
        {
            return;
        }

        DissipateRing(entity);
    }

    private void OnShutdown(Entity<SmokeRingComponent> entity, ref ComponentShutdown args)
    {
        if (!entity.Comp.Dissipated)
            DissipateRing(entity);
    }

    /// <summary>
    /// Создает и запускает колечко дыма или пара в целевом направлении.
    /// </summary>
    public EntityUid? SpawnSmokeRing(EntityUid user, EntityCoordinates? target = null, Gas gasType = Gas.WaterVapor)
    {
        var userXform = Transform(user);
        Vector2 dir;

        if (target != null && target.Value.IsValid(EntityManager))
        {
            var userMapCoords = _transform.GetMapCoordinates(user, userXform);
            var targetMapCoords = _transform.ToMapCoordinates(target.Value);
            if (userMapCoords.MapId == targetMapCoords.MapId)
            {
                var diff = targetMapCoords.Position - userMapCoords.Position;
                if (diff.LengthSquared() > 0.001f)
                    dir = Vector2.Normalize(diff);
                else
                    dir = _transform.GetWorldRotation(userXform).ToWorldVec();
            }
            else
            {
                dir = _transform.GetWorldRotation(userXform).ToWorldVec();
            }
        }
        else
        {
            dir = _transform.GetWorldRotation(userXform).ToWorldVec();
        }

        if (dir.LengthSquared() > 0.001f)
            dir = Vector2.Normalize(dir);
        else
            dir = new Vector2(0, 1);

        var userMap = _transform.GetMapCoordinates(user, userXform);
        var spawnMapCoords = userMap.Offset(dir * 0.45f);
        var ring = Spawn(SmokeRingPrototype, spawnMapCoords);

        if (TryComp<SmokeRingComponent>(ring, out var smokeRing))
        {
            smokeRing.Shooter = user;
            smokeRing.GasType = gasType;
            if (TryComp<BuckleComponent>(user, out var buckle) && buckle.BuckledTo != null)
                smokeRing.IgnoredSeat = buckle.BuckledTo;
        }

        _throwing.TryThrow(ring, dir * 4.5f, baseThrowSpeed: 4.5f, user: user, pushbackRatio: 0f, friction: 0.02f, playSound: false, doSpin: false);
        return ring;
    }

    /// <summary>
    /// Растворяет колечко в атмосферный газ (водяной пар или табачный дым) без спавна эффектов.
    /// </summary>
    public void DissipateRing(Entity<SmokeRingComponent> entity)
    {
        if (entity.Comp.Dissipated)
            return;

        entity.Comp.Dissipated = true;

        var environment = _atmos.GetContainingMixture(entity.Owner, true, true);
        if (environment != null)
        {
            var merger = new GasMixture(1) { Temperature = Atmospherics.T20C + 15f };
            merger.SetMoles(entity.Comp.GasType, 1.8f);
            _atmos.Merge(environment, merger);
        }

        QueueDel(entity.Owner);
    }
}
