using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Traits.Assorted;

public sealed partial class ImpairedMobilitySystem
{
    private bool IsMobilityAidActive(EntityUid item)
    {
        return !TryComp<ItemToggleComponent>(item, out var toggle) || !toggle.Activated;
    }
}
