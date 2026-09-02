using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Lust.LockableEquipment;

/// <summary>
/// Adds a temporary activated icon pulse when a lockable device is triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(ElectricLockableEquipmentSystem))]
public sealed partial class ElectricLockableEquipmentComponent : Component
{
    /// <summary>
    /// RSI state shown on the item itself while the device is actively shocking.
    /// This is useful when the device is visible in hands or inventory.
    /// </summary>
    [DataField]
    public string ActivatedIconState = "icon_activated";

    /// <summary>
    /// How long the activated icon state stays visible after a trigger.
    /// </summary>
    [DataField]
    public TimeSpan ActivationDuration = TimeSpan.FromSeconds(0.35);

    /// <summary>
    /// Internal timer used to restore the default lockable icon state.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan ActivatedUntil = TimeSpan.Zero;
}
