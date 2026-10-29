using System;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CombatMode;

[Serializable, NetSerializable]
public sealed class CombatHitFeedbackEvent(CombatHitFeedbackType type) : EntityEventArgs
{
    public CombatHitFeedbackType Type { get; } = type;
}
