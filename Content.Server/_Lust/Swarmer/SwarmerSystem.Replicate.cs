using Content.Shared._Lust.Swarmer;
using Content.Shared._Lust.Swarmer.Components;
using Content.Shared.DoAfter;

namespace Content.Server._Lust.Swarmer;

public sealed partial class SwarmerSystem
{
    private void InitializeReplicate()
    {
        SubscribeLocalEvent<SwarmerComponent, SwarmerReplicateActionEvent>(OnReplicateAction);
        SubscribeLocalEvent<SwarmerComponent, SwarmerReplicateDoAfterEvent>(OnReplicateDoAfter);
    }

    private void OnReplicateAction(Entity<SwarmerComponent> ent, ref SwarmerReplicateActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartReplicate(ent);
    }

    private void OnReplicateDoAfter(Entity<SwarmerComponent> ent, ref SwarmerReplicateDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = TryReplicate(ent);
    }

    /// <summary>
    /// Starts assembling a copy of the swarmer while it holds still.
    /// </summary>
    public bool TryStartReplicate(Entity<SwarmerComponent> ent)
    {
        if (!CanReplicate(ent))
            return false;

        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.ReplicateDelay, new SwarmerReplicateDoAfterEvent(), ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        if (!DoAfter.TryStartDoAfter(doAfter))
            return false;

        PopupSelf(ent, Loc.GetString("swarmer-replicate-start"));
        return true;
    }

    /// <summary>
    /// Builds a full copy of the swarmer. It starts dormant and empty, waiting for a ghost to take it over.
    /// </summary>
    public bool TryReplicate(Entity<SwarmerComponent> ent)
    {
        if (!CanReplicate(ent))
            return false;

        ent.Comp.Resources -= ent.Comp.ReplicateCost;
        Dirty(ent);

        var replica = SpawnAtPosition(ent.Comp.ReplicaPrototype, TransformSystem.GetMoverCoordinates(ent));
        if (TryComp<SwarmerComponent>(replica, out var replicaComp))
        {
            replicaComp.Resources = ent.Comp.ReplicaStartingResources;
            Dirty(replica, replicaComp);
        }

        PopupSelf(ent, Loc.GetString("swarmer-replicate-done"));
        return true;
    }

    /// <summary>
    /// Whether the swarmer can pay for a copy. Checked again after the do-after, since resources may have been spent meanwhile.
    /// </summary>
    public bool CanReplicate(Entity<SwarmerComponent> ent, bool quiet = false)
    {
        return HasResources(ent, ent.Comp.ReplicateCost, quiet);
    }
}
