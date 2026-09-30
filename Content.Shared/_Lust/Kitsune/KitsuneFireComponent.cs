using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Lust.Kitsune
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class KitsuneFireComponent : Component
    {
        [DataField]
        public DamageSpecifier Healing = new()
        {
            DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
            {
                { "Heat", -2 },
                { "Cold", -2 },
                { "Shock", -2 },
            },
        };

        [DataField("duration", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan Duration;

        [DataField("nextTick", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextTick;
    }
}
