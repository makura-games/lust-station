using Robust.Shared.GameObjects;

namespace Content.Shared._Lust.Smell;

/// <summary>
/// Broadcast event from the interactions panel life system: the entity has crossed
/// the arousal threshold and is now "emitting" it. Raised by the ERP system;
/// subscribers refresh the corresponding temporary scent on the bearer.
/// </summary>
public sealed class ArousalStartedEvent : EntityEventArgs
{
    public EntityUid Uid;
}

/// <summary>
/// Broadcast event of the life system: the entity had an orgasm. User — who finished,
/// Target — the receiver of the effect (the partner or the user themselves). Raised by the ERP system.
/// </summary>
public sealed class OrgasmPerformedEvent : EntityEventArgs
{
    public EntityUid User;
    public EntityUid Target;
}
