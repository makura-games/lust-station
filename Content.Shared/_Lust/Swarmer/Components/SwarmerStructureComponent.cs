using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Swarmer.Components;

/// <summary>
/// A structure built by swarmers (barricade, trap). Only one can stand on a tile.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SwarmerStructureComponent : Component
{
    /// <summary>
    /// If true, swarmers and projectiles fired by swarmers pass through this structure.
    /// </summary>
    [DataField]
    public bool SwarmerPassable;
}
