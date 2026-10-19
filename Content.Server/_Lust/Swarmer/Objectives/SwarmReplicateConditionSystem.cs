using Content.Server._Lust.Swarmer.Rules;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Server._Lust.Swarmer.Objectives;

/// <summary>
/// Tracks how close the Swarm is to its target size.
/// </summary>
public sealed partial class SwarmReplicateConditionSystem : EntitySystem
{
    [Dependency] private NumberObjectiveSystem _number = default!;
    [Dependency] private SwarmRuleSystem _swarmRule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwarmReplicateConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<SwarmReplicateConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(ent);
        args.Progress = target <= 0 ? 1f : MathF.Min(1f, (float) _swarmRule.CountAliveSwarmers() / target);
    }
}
