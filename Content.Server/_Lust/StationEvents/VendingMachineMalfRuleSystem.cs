using Content.Server.StationEvents.Components;
using Content.Server.VendingMachines;
using Content.Shared.GameTicking.Components;
using Content.Shared.VendingMachines;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed partial class VendingMachineMalfRuleSystem
    : StationEventSystem<VendingMachineMalfRuleComponent>
{
    [Dependency] private VendingMachineSystem _vending = default!;

    protected override void Started(
        EntityUid uid,
        VendingMachineMalfRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
            return;

        var candidates = new List<EntityUid>();
        var query =
            EntityQueryEnumerator<VendingMachineComponent, TransformComponent>();

        while (query.MoveNext(out var machineUid, out var vending, out var xform))
        {
            if (!xform.Anchored || vending.Broken)
                continue;

            if (StationSystem.GetOwningStation(machineUid, xform) != station)
                continue;

            candidates.Add(machineUid);
        }

        if (candidates.Count == 0)
            return;

        var machinesToUse = Math.Min(
            RobustRandom.Next(component.MinMachines, component.MaxMachines + 1),
            candidates.Count);

        for (var i = 0; i < machinesToUse; i++)
        {
            var machineUid = RobustRandom.PickAndTake(candidates);
            var itemsToEject =
                RobustRandom.Next(component.MinItems, component.MaxItems + 1);

            for (var j = 0; j < itemsToEject; j++)
            {
                _vending.EjectRandom(
                    machineUid,
                    throwItem: true,
                    forceEject: true);
            }
        }
    }
}