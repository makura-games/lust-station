using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Lust.Kitsune
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class KitsuneFoxLightsComponent : Component
    {
        [DataField]
        public List<EntityUid> Orbs = [];

        [DataField("lightDuration")]
        public TimeSpan LightDuration = TimeSpan.FromSeconds(90);

        [DataField("castTime")]
        public TimeSpan CastTime = TimeSpan.FromSeconds(1);
    }

    [RegisterComponent, NetworkedComponent]
    public sealed partial class KitsuneFoxLightsOrbComponent : Component
    {
        [DataField]
        public float Angle;

        [DataField]
        public float Speed = 2f;

        [DataField]
        public float Radius = 1f;

        [DataField]
        public EntityUid Parent;

        [DataField("dieAt", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan DieAt;
    }
}
