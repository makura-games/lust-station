using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Prototypes;

/// <summary>
/// Сопоставляет игровое событие (trigger) с временным запахом,
/// который нужно применить к сущности.
/// </summary>
[Prototype("scentEvent")]
public sealed partial class ScentEventPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Строка-ключ события: ID реагента, типа взаимодействия, категории и т.п.
    /// По нему SmellSystem находит нужный прототип.
    /// </summary>
    [DataField(required: true)]
    public string Trigger { get; private set; } = default!;

    /// <summary>
    /// Какой запах добавляется.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ScentPrototype> Scent { get; private set; }

    /// <summary>
    /// Сколько времени запах держится.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Duration { get; private set; }

    /// <summary>
    /// Начальная сила запаха 0..1.
    /// </summary>
    [DataField]
    public float Intensity { get; private set; } = 1f;

    /// <summary>
    /// К кому применяется запах: user / target / both.
    /// У trigger-ивентов, где двух участников нет, это "self".
    /// </summary>
    [DataField]
    public ScentApplyTarget ApplyTo { get; private set; } = ScentApplyTarget.Self;

    /// <summary>
    /// Прогрев: сколько времени нужно, чтобы запах "развернулся" до полной силы (для состояний).
    /// </summary>
    [DataField]
    public TimeSpan RampUp { get; private set; }
}

public enum ScentApplyTarget
{
    Self,
    User,
    Target,
    Both,
}