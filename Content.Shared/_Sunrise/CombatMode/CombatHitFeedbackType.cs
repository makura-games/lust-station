using System;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CombatMode;

[Serializable, NetSerializable]
public enum CombatHitFeedbackType : byte
{
    Hit,
    Critical,
    Kill,
}
