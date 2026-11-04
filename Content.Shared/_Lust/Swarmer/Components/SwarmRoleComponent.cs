using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Swarmer.Components;

/// <summary>
/// Added to mind role entities to tag that they are part of the Swarm.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SwarmRoleComponent : BaseMindRoleComponent;
