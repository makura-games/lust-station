using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._Sunrise.Smoking;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition;
using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Server._Sunrise.Smoking;

/// <summary>
/// Серверная часть системы вейпов: выполнение затяжек, взрывы, передача жидкостей в кровоток и испарение.
/// </summary>
public sealed partial class VapeSystem : SharedVapeSystem
{
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private ReactiveSystem _reactive = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SmokeRingSystem _smokeRing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VapeComponent, VapeDoAfterEvent>(OnVapeDoAfter);
        SubscribeLocalEvent<VapeComponent, SmokeRingDoAfterEvent>(OnVapeSmokeRingDoAfter);
        SubscribeLocalEvent<VapeComponent, GotEmaggedEvent>(OnVapeEmagged);
    }

    private void OnVapeDoAfter(Entity<VapeComponent> entity, ref VapeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;
        var user = args.Args.User;

        if (entity.Comp.ExplodeOnUse || _emag.CheckFlag(entity, EmagType.Interaction))
        {
            _explosion.QueueExplosion(entity.Owner, "Default", entity.Comp.ExplosionIntensity, 0.5f, 3, canCreateVacuum: false);
            Del(entity);
            args.Handled = true;
            return;
        }

        if (!CanVape(entity, user, target, out var cartridgeUid, out var solution, checkCooldown: false))
            return;

        if (cartridgeUid == null || solution == null)
            return;

        if (!TryComp<VapeCartridgeComponent>(cartridgeUid.Value, out var cartridge) ||
            !Solution.TryGetSolution(cartridgeUid.Value, cartridge.Solution, out var soln, out _))
            return;

        // Проверка на взрывоопасные реагенты (плазма, тритий)
        foreach (var reagent in solution.Contents)
        {
            if (entity.Comp.ExplosiveReagents.Contains(reagent.Reagent.Prototype))
            {
                _explosion.QueueExplosion(entity.Owner, "Default", entity.Comp.ExplosionIntensity * 1.5f, 1f, 4, canCreateVacuum: false);
                Del(entity);
                args.Handled = true;
                return;
            }
        }

        var puffVol = FixedPoint2.Min(FixedPoint2.New(entity.Comp.PuffVolume), solution.Volume);
        var split = Solution.SplitSolution(soln.Value, puffVol);

        if (split.Volume > FixedPoint2.Zero)
        {
            _reactive.DoEntityReaction(target, split, ReactionMethod.Ingestion);
            _bloodstream.TryAddToBloodstream(target, split);
        }

        if (entity.Comp.Damage != null)
        {
            _damageable.TryChangeDamage(target, entity.Comp.Damage, true);
        }

        // Выделение пара в атмосферу
        var environment = _atmos.GetContainingMixture(target, true, true);
        if (environment != null)
        {
            var merger = new GasMixture(1) { Temperature = Atmospherics.T20C + 20f };
            merger.SetMoles(entity.Comp.GasType, entity.Comp.VaporMolesPerPuff);
            _atmos.Merge(environment, merger);
        }

        if (entity.Comp.ExhaleSound != null)
        {
            Audio.PlayPvs(entity.Comp.ExhaleSound, target, AudioParams.Default.WithVolume(-1.5f).WithMaxDistance(7f).WithVariation(0.04f));
        }

        if (args.Forced)
        {
            var targetName = Identity.Entity(target, EntityManager);
            var userName = Identity.Entity(user, EntityManager);
            Popup.PopupEntity(Loc.GetString("vape-component-vape-success-forced", ("user", userName)), target, target);
            Popup.PopupEntity(Loc.GetString("vape-component-vape-success-user-forced", ("target", targetName)), user, user);
        }
        else
        {
            Popup.PopupEntity(Loc.GetString("vape-component-vape-success"), target, target);
        }

        args.Handled = true;
    }

    private void OnVapeSmokeRingDoAfter(Entity<VapeComponent> entity, ref SmokeRingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var targetCoords = GetCoordinates(args.TargetCoordinates);
        if (TryFinalizeSmokeRing(entity, args.Args.User, targetCoords))
            args.Handled = true;
    }

    public bool TryFinalizeSmokeRing(
        Entity<VapeComponent> entity,
        EntityUid user,
        EntityCoordinates targetCoords)
    {
        if (!CanFinalizeSmokeRing(entity, user, out var cartridgeUid, out var cartridge, out var soln, out var solution))
            return false;

        DoFinalizeSmokeRing(entity, user, targetCoords, cartridgeUid, cartridge, soln, solution);
        return true;
    }

    public bool CanFinalizeSmokeRing(
        Entity<VapeComponent> entity,
        EntityUid user,
        out EntityUid? cartridgeUid,
        out VapeCartridgeComponent? cartridge,
        out Entity<SolutionComponent>? soln,
        out Solution? solution)
    {
        cartridge = null;
        soln = null;

        if (entity.Comp.ExplodeOnUse || _emag.CheckFlag(entity, EmagType.Interaction))
        {
            CanVape(entity, user, user, out cartridgeUid, out solution, checkCooldown: false);
            return true;
        }

        if (!CanVape(entity, user, user, out cartridgeUid, out solution, checkCooldown: false) ||
            cartridgeUid == null || solution == null)
        {
            return false;
        }

        if (!TryComp(cartridgeUid.Value, out cartridge) ||
            !Solution.TryGetSolution(cartridgeUid.Value, cartridge.Solution, out var solutionEnt, out _))
        {
            return false;
        }

        soln = solutionEnt;
        return true;
    }

    public void DoFinalizeSmokeRing(
        Entity<VapeComponent> entity,
        EntityUid user,
        EntityCoordinates targetCoords,
        EntityUid? cartridgeUid,
        VapeCartridgeComponent? cartridge,
        Entity<SolutionComponent>? soln,
        Solution? solution)
    {
        if (entity.Comp.ExplodeOnUse || _emag.CheckFlag(entity, EmagType.Interaction))
        {
            _explosion.QueueExplosion(entity.Owner, "Default", entity.Comp.ExplosionIntensity, 0.5f, 3, canCreateVacuum: false);
            Del(entity);
            return;
        }

        if (solution != null && soln != null)
        {
            // Проверка на взрывоопасные реагенты (плазма, тритий)
            foreach (var reagent in solution.Contents)
            {
                if (entity.Comp.ExplosiveReagents.Contains(reagent.Reagent.Prototype))
                {
                    _explosion.QueueExplosion(entity.Owner, "Default", entity.Comp.ExplosionIntensity * 1.5f, 1f, 4, canCreateVacuum: false);
                    Del(entity);
                    return;
                }
            }

            var puffVol = FixedPoint2.Min(FixedPoint2.New(entity.Comp.PuffVolume), solution.Volume);
            var split = Solution.SplitSolution(soln.Value, puffVol);

            if (split.Volume > FixedPoint2.Zero)
            {
                _reactive.DoEntityReaction(user, split, ReactionMethod.Ingestion);
                _bloodstream.TryAddToBloodstream(user, split);
            }
        }

        if (entity.Comp.ExhaleSound != null)
        {
            Audio.PlayPvs(entity.Comp.ExhaleSound, user, AudioParams.Default.WithVolume(-1.5f).WithMaxDistance(7f).WithVariation(0.04f));
        }

        Popup.PopupEntity(Loc.GetString("smoking-puff-smoke-ring", ("user", Identity.Entity(user, EntityManager))), user);
        _smokeRing.SpawnSmokeRing(user, targetCoords, entity.Comp.GasType);
    }

    private void OnVapeEmagged(Entity<VapeComponent> entity, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(entity, EmagType.Interaction))
            return;

        entity.Comp.ExplodeOnUse = true;
        args.Handled = true;
    }
}
