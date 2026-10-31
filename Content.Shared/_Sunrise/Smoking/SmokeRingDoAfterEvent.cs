using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Smoking;

/// <summary>
/// Событие DoAfter для затяжки перед выдуванием колечка дыма или пара.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SmokeRingDoAfterEvent : SimpleDoAfterEvent
{
    public NetCoordinates TargetCoordinates;

    public SmokeRingDoAfterEvent()
    {
    }

    public SmokeRingDoAfterEvent(NetCoordinates targetCoordinates)
    {
        TargetCoordinates = targetCoordinates;
    }
}
