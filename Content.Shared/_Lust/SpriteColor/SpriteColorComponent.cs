using Robust.Shared.GameStates;

namespace Content.Shared._Lust.SpriteColor;

/// <summary>
/// Component that stores information about which sprite states should be colored and what color to apply.
/// Used for applying runtime color changes to specific sprite states (e.g., player hair color to fox form fur).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpriteColorComponent : Component
{
    /// <summary>
    /// Dictionary mapping sprite state names to the colors that should be applied to them.
    /// Example: { "nine-tail_fox_gray_color" -> hair color }
    /// </summary>
    [DataField("stateColors"), AutoNetworkedField]
    public Dictionary<string, Color> StateColors = new();
}
