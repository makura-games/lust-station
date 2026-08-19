using Content.Shared._Lust.Smell.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Components;

[RegisterComponent]
public sealed partial class ScentComponent : Component
{
    [DataField]
    public List<ProtoId<ScentPrototype>> BaseScents = new();

    [DataField]
    public ProtoId<PersonalScentProfilePrototype>? PersonalScentProfile;

    /// <summary>
    /// Временные запахи: события добавили запись, а протухание и силу
    /// вычисляем лениво при запросе. Хранится и обрабатывается на сервере.
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
