using Content.Shared.Cloning.Components;
using Content.Shared.Mind;

namespace Content.Server.Cloning;

public sealed class ParadoxCloneImmuneSystem : EntitySystem
{
    public void RemoveInvalidTargets(HashSet<Entity<MindComponent>> targets)
    {
        targets.RemoveWhere(mind =>
            mind.Comp.OwnedEntity is not { } body ||
            HasComp<ParadoxCloneImmuneComponent>(body));
    }
}
