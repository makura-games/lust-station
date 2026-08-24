using Content.Shared._Lust.Smell.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell;

/// <summary>
/// Запись об активном временном запахе носителя: что пахнет, с какого момента
/// и как долго. Хранится в ScentComponent.TemporaryScents.
/// </summary>
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
    /// Длительность действия временного запаха: у каждого источника своя.
    /// Интенсивность берётся из прототипа самого запаха (единая для всех источников).
    /// </summary>
    [DataField]
    public TimeSpan Duration;
}
