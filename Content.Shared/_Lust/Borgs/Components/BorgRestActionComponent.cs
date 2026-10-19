using Content.Shared._Lust.Borgs;
using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Borgs.Components;

/// <summary>
/// Adds a borg action that toggles voluntary resting.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBorgRestActionSystem))]
public sealed partial class BorgRestActionComponent : Component
{
    /// <summary>
    /// Action prototype used to sit down or stand up.
    /// </summary>
    [DataField]
    public EntProtoId ToggleAction = "ActionBorgToggleRest";

    /// <summary>
    /// Action entity currently granted to the borg.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? ToggleActionEntity;
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBorgRestActionSystem))]
public sealed partial class BorgRestingComponent : Component;

public sealed partial class BorgToggleRestActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum BorgRestVisuals : byte
{
    Resting,
}
