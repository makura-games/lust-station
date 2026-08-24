using Content.Shared._Lust.Smell.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Components;
/// <summary>
/// Носитель запахов: статичные базовые и личный аромат, временные запахи
/// от источников и состояние маскировки основного запаха.
/// </summary>
[RegisterComponent]
public sealed partial class ScentComponent : Component
{
    /// <summary>
    /// Статичные (базовые) запахи носителя, всегда присутствующие при осмотре:
    /// видовые/расовые и прочие постоянные ноты.
    /// </summary>
    [DataField]
    public List<ProtoId<ScentPrototype>> BaseScents = new();

    /// <summary>
    /// Профиль для генерации личного запаха (цвет + ноты) из характеристик
    /// персонажа (имя, возраст, пол, голос). Если не задан — личного запаха нет.
    /// </summary>
    [DataField]
    public ProtoId<PersonalScentProfilePrototype>? PersonalScentProfile;

    /// <summary>
    /// Список временных запахов
    /// </summary>
    [DataField]
    public List<ActiveTemporaryScent> TemporaryScents = new();

    /// <summary>
    /// Активна ли временная маскировка основного запаха (например, после мытья мылом).
    /// Пока активна — основной (статичный + личный) запах скрыт от нюхающих,
    /// временные запахи при этом продолжают показываться.
    /// </summary>
    [DataField]
    public bool Masked;

    /// <summary>
    /// Игровой момент, до которого действует маскировка. После истечения
    /// маскировка снимается лениво при очередном нюхании.
    /// </summary>
    [DataField]
    public TimeSpan MaskUntil;

}
