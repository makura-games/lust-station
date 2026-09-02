using Robust.Shared.GameObjects;

namespace Content.Shared._Lust.LockableEquipment;

/// <summary>
/// Raised on the device entity before it is inserted into an equipment container.
/// Cancel to block the attachment and optionally set <see cref="Reason"/> to a loc-id
/// that will be shown as a popup to the user.
/// </summary>
public sealed class EquipmentContainerAttachAttemptEvent(EntityUid target, EntityUid user) : CancellableEntityEventArgs
{
    /// <summary>The entity the device is being attached to.</summary>
    public readonly EntityUid Target = target;

    /// <summary>The entity performing the attachment.</summary>
    public readonly EntityUid User = user;

    /// <summary>Optional loc-id shown as a popup when the event is cancelled.</summary>
    public string? Reason;
}
