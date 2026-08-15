using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Prototypes;

[Prototype("scent")]
public sealed partial class ScentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Description { get; private set; } = default!;

    [DataField]
    public Color? Color { get; private set; }

    /// <summary>
    /// Интенсивность запаха 0..1 — единая для всех источников этого запаха
    /// (раньше задавалась в каждом источнике, теперь живёт в самом прототипе).
    /// </summary>
    [DataField]
    public float Intensity { get; private set; } = 1f;

    /// <summary>
    /// Выводить описание этого запаха жирным шрифтом (акцентный/резкий запах).
    /// </summary>
    [DataField]
    public bool Fat { get; private set; }
}
