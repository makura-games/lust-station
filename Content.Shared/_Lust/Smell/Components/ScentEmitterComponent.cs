using Content.Shared._Lust.Smell.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Components;

/// <summary>
/// Маркер-эмитор: предмет, попав в нужный слот инвентаря, даёт носителю
/// временный запах. Настройки (слот, запах, длительность, сила) задаются
/// прямо на предмете в YAML через поля этого компонента.
/// </summary>
[RegisterComponent]
public sealed partial class ScentEmitterComponent : Component
{
    /// <summary>
    /// В какой слот должен попасть предмет, чтобы испустить запах.
    /// </summary>
    [DataField]
    public string Slot = "mask";

    /// <summary>
    /// Какой запах даём (id из scents.yml).
    /// </summary>
    [DataField]
    public ProtoId<ScentPrototype> Scent = default!;

    /// <summary>
    /// Сколько времени запах держится на носителе.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Начальная сила запаха 0..1.
    /// </summary>
    [DataField]
    public float Intensity = 0.8f;
}