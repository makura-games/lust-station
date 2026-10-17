using Content.Shared._Lust.Thermite.Components;

namespace Content.Shared._Lust.Thermite.Systems;

public abstract class SharedDissolvableSystem : EntitySystem
{
    public void AdjustDissolveStacks(EntityUid uid, float delta, DissolvableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var stacks = Math.Clamp(component.Stacks + delta, component.MinimumStacks, component.MaximumStacks);
        if (component.Stacks == stacks)
            return;

        component.Stacks = stacks;
        DirtyField(uid, component, nameof(DissolvableComponent.Stacks));
    }
}
