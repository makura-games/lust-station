using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Smoking;

[Serializable, NetSerializable]
public sealed partial class SmokablePuffDoAfterEvent : SimpleDoAfterEvent
{
    public readonly bool Forced;

    public SmokablePuffDoAfterEvent(bool forced = false)
    {
        Forced = forced;
    }
}
