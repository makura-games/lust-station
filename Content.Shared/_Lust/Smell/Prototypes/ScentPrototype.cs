using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Prototypes;

/// <summary>
/// Прототип запаха: локализованное описание, цвет в тултипе нюха,
/// интенсивность для сортировки внутри групп силы и флаг жирного вывода.
/// </summary>
[Prototype]
public sealed partial class ScentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// LocId описания запаха.
    /// </summary>
    [DataField(required: true)]
    public LocId Description { get; private set; } = default!;

    /// <summary>
    /// Цвет текста запаха; null — без выделения.
    /// </summary>
    [DataField]
    public Color? Color { get; private set; }

    /// <summary>
    /// Интенсивность запаха 0..1 — единая для всех источников этого запаха
    /// </summary>
    [DataField]
    public float Intensity { get; private set; } = 1f;

    /// <summary>
    /// Выводить описание этого запаха жирным шрифтом (акцентный/резкий запах).
    /// </summary>
    [DataField]
    public bool Fat { get; private set; }
}
