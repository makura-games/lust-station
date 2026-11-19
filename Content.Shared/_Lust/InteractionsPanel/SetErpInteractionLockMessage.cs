using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.InteractionsPanel;

[Serializable, NetSerializable]
public sealed class SetErpInteractionLockMessage : BoundUserInterfaceMessage
{
    public bool Locked { get; }

    public SetErpInteractionLockMessage(bool locked)
    {
        Locked = locked;
    }
}