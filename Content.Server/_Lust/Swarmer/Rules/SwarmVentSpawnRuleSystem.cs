using Content.Server.Antag;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Collections;

namespace Content.Server._Lust.Swarmer.Rules;

/// <summary>
/// Picks a random air vent of a random station as the spawn point for this rule's antagonists.
/// </summary>
public sealed partial class SwarmVentSpawnRuleSystem : StationEventSystem<SwarmVentSpawnRuleComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwarmVentSpawnRuleComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(EntityUid uid, SwarmVentSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, gameRule, args);

        // Место выбираем здесь: событие выбора места приходит дважды (для спавнера и для занявшего роль игрока).
        if (!TryGetRandomStation(out var station))
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        var vents = new ValueList<EntityUid>();
        var query = EntityQueryEnumerator<GasVentPumpComponent, TransformComponent>();
        while (query.MoveNext(out var vent, out _, out var xform))
        {
            if (!xform.Anchored || CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != station)
                continue;

            vents.Add(vent);
        }

        if (vents.Count == 0)
        {
            Log.Warning($"No air vents found on {ToPrettyString(station)} for {ToPrettyString(uid)}");
            ForceEndSelf(uid, gameRule);
            return;
        }

        comp.Coords = _transform.GetMapCoordinates(vents[RobustRandom.Next(vents.Count)]);
    }

    private void OnSelectLocation(Entity<SwarmVentSpawnRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords is { } coords)
            args.Coordinates.Add(coords);
    }
}
