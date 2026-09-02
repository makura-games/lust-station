using Robust.Shared.GameStates;

namespace Content.Shared._Lust.LockableEquipment;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(LockableEquipmentSystem))]
public sealed partial class KeyComponent : Component
{
    /// <summary>
    /// Shared lock identifier paired with a lockable device.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? LockId;
}
