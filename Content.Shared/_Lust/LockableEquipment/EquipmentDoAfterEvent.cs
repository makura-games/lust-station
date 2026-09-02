using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Lust.LockableEquipment;

public enum EquipmentActionType
{
    Attach,
    Detach
}

[Serializable, NetSerializable]
public sealed partial class EquipmentDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public EquipmentActionType Action;

    public EquipmentDoAfterEvent(EquipmentActionType action) => Action = action;

    private EquipmentDoAfterEvent() { }
}
