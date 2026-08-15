using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Prototypes;

/// <summary>
/// Сопоставляет статус-эффект (пьянство, наркотрип и т.п.) с запахом состояния.
/// Пока эффект активен, носитель пахнет соответствующим запахом.
/// Сила запаха (Strong/Medium/Faint) определяется положением внутри времени эффекта:
/// чем дальше до конца эффекта, тем сильнее. Нормируется по собственной длительности эффекта.
/// </summary>
[Prototype("statusScent")]
public sealed partial class StatusScentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Прототип статус-эффекта, наличие которого включает этот запах.
    /// Например "StatusEffectDrunk".
    /// </summary>
    [DataField(required: true)]
    public EntProtoId StatusEffect { get; private set; }

    /// <summary>
    /// Какой запах источает носитель (групповой: алкоголь, стимуляторы, наркотики).
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ScentPrototype> Scent { get; private set; }

    /// <summary>
    /// Минимальная длительность эффекта, при которой запах способен достичь Strong.
    /// Короткие эффекты (глоток выпивки) не должны сильно пахнуть: их максимум — Medium.
    /// </summary>
    [DataField]
    public TimeSpan MinDurationForStrong { get; private set; } = TimeSpan.FromSeconds(60);
}