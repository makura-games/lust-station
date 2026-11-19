using Robust.Shared.Map;

namespace Content.Server._Lust.Swarmer.Rules;

/// <summary>
/// Spawns this rule's antagonists at a random air vent of a random station.
/// </summary>
[RegisterComponent]
public sealed partial class SwarmVentSpawnRuleComponent : Component
{
    /// <summary>
    /// The vent picked when the rule was added, so every spawner of this rule uses the same one.
    /// </summary>
    [ViewVariables]
    public MapCoordinates? Coords;
}
