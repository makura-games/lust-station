using Content.Shared._Lust.Smell.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Components;

/// <summary>
/// Маркер-эмитор: предмет, взятый в руки или надетый в слот инвентаря, даёт
/// носителю временный запах. Настройки (куда/как реагировать, запах, длительность,
/// сила) задаются прямо на предмете в YAML через поля этого компонента.
/// </summary>
[RegisterComponent]
public sealed partial class ScentEmitterComponent : Component
{
    /// <summary>
    /// Куда должен попасть предмет, чтобы испустить запах.
    /// </summary>
    [DataField]
    public ScentEmitSpot Spot = ScentEmitSpot.SpecificSlot;

    /// <summary>
    /// Конкретный слот, если Spot = SpecificSlot (например "mask").
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
}

/// <summary>
/// Определяет, при каком контакте с игроком предмет-эмитор испускает запах.
/// </summary>
public enum ScentEmitSpot
{
    /// <summary>
    /// Только в конкретном слоте одежды (поле Slot). Например сигарета во рту.
    /// </summary>
    SpecificSlot,

    /// <summary>
    /// В любом слоте одежды. Например взрывчатка, запах которой везде заметен.
    /// </summary>
    AnySlot,

    /// <summary>
    /// Только в руках.
    /// </summary>
    Hands,
}