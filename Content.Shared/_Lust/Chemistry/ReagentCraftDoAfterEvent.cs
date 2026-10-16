using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Chemistry;

[Serializable, NetSerializable]
public sealed partial class ReagentCraftDoAfterEvent : DoAfterEvent
{
    public int Recipe;

    public override DoAfterEvent Clone() => new ReagentCraftDoAfterEvent
    {
        Recipe = Recipe,
        Repeat = Repeat,
        Handled = Handled,
    };
}
