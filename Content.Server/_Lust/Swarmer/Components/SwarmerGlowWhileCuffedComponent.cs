namespace Content.Server._Lust.Swarmer.Components;

/// <summary>
/// Put on restraints: whoever wears them glows like a swarmer.
/// Lights of contained entities aren't rendered, so the light has to go on the wearer.
/// </summary>
[RegisterComponent]
public sealed partial class SwarmerGlowWhileCuffedComponent : Component
{
    /// <summary>
    /// Color of the wearer's glow.
    /// </summary>
    [DataField]
    public Color Color = Color.FromHex("#00FFFF");

    /// <summary>
    /// Radius of the wearer's glow, in tiles.
    /// </summary>
    [DataField]
    public float Radius = 2f;

    /// <summary>
    /// Brightness of the wearer's glow.
    /// </summary>
    [DataField]
    public float Energy = 1f;

    /// <summary>
    /// The wearer we gave a light to, so it can be taken away again.
    /// </summary>
    [ViewVariables]
    public EntityUid? LitWearer;
}
