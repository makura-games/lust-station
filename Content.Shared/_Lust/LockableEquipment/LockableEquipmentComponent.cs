using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.LockableEquipment;

/// <summary>
/// Stores lock, break and visual configuration for a lockable device.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(LockableEquipmentSystem), typeof(EquipmentContainerSystem))]
public sealed partial class LockableEquipmentComponent : Component
{
    /// <summary>
    /// Whether the device is currently locked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Locked;

    /// <summary>
    /// Whether the device is currently broken and must be repaired.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Broken;

    /// <summary>
    /// Shared identifier paired with a matching key.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? LockId;

    /// <summary>
    /// Body layer occupied by the installed device.
    /// </summary>
    [DataField]
    public string Layer = "lockable_under";

    /// <summary>
    /// RSI path used for the installed device overlay.
    /// </summary>
    [DataField]
    public string? RsiPath;

    /// <summary>
    /// Delay before attaching this device completes.
    /// </summary>
    [DataField]
    public TimeSpan AttachDoAfter = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Delay before removing this device completes.
    /// </summary>
    [DataField]
    public TimeSpan DetachDoAfter = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Forced-open behavior used when the device is broken open.
    /// </summary>
    [DataField]
    public BreakMode Mode = BreakMode.Breakable;

    /// <summary>
    /// Tag required on a tool to force the device open.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> RequiredToolTag = "Wirecutter";

    /// <summary>
    /// Delay before a forced-open attempt completes.
    /// </summary>
    [DataField]
    public TimeSpan BreakDoAfter = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Stack material required to repair a broken device.
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype>? RepairMaterial;

    /// <summary>
    /// Amount of repair material consumed on repair.
    /// </summary>
    [DataField]
    public int RepairAmount = 1;
    
    /// <summary>
    /// RSI state used for the installed device overlay.
    /// </summary>
    [DataField]
    public string SpriteState = "equipped";
    
    /// <summary>
    /// Defines what happens when the device is forced open.
    /// </summary>
    public enum BreakMode
    {
        None,
        Breakable,
        Destroyable,
        ForceOpen
    }
}
