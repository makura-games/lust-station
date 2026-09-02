using Robust.Shared.GameStates;

namespace Content.Shared._Lust.LockableEquipment;

/// <summary>
/// Stores the internal container used for installed lockable devices.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(EquipmentContainerSystem))]
public sealed partial class EquipmentContainerComponent : Component
{
    /// <summary>
    /// Container identifier holding the currently installed device.
    /// </summary>
    [DataField]
    public string ContainerId = "locked-equipment";
}
