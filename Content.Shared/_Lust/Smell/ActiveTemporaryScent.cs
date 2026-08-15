using Content.Shared._Lust.Smell.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell;

[DataDefinition, Serializable]
public sealed partial class ActiveTemporaryScent
{
    /// <summary>
    /// Какой запах был применён.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ScentPrototype> Scent;

    /// <summary>
    /// Игровой момент, когда запах появился.
    /// </summary>

    [DataField]
    public TimeSpan StartTime;

    /// <summary>
    /// Сколько всего времени запах живёт.
    /// </summary>
    [DataField]
    public TimeSpan Duration;

    /// <summary>
    /// Исходная сила запаха 0..1. Определяет порядок внутри группы силы.
    /// </summary>
    [DataField]
    public float Intensity = 1f;
}
