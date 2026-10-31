using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Weapons.Melee;

/// <summary>
/// Компонент оружия, позволяющий отрубать конечности и головы при попадании в ближнем бою.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DismemberingWeaponComponent : Component
{
    /// <summary>
    /// Шанс отрубания обычной конечности (рука, нога, кисть, стопа). По умолчанию 15%.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LimbDismemberChance = 0.15f;

    /// <summary>
    /// Отдельный шанс отсечения головы (поменьше). По умолчанию 3%.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeadDismemberChance = 0.03f;

    /// <summary>
    /// Сила/скорость отлетания отрубленной конечности в метрах/тайлах.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EjectForce = 3.5f;

    /// <summary>
    /// Звук отсечения плоти / разрубания.
    /// </summary>
    [DataField]
    public SoundSpecifier DismemberSound = new SoundPathSpecifier("/Audio/Weapons/bladeslice.ogg");

    /// <summary>
    /// Количество крови в основной луже при отсечении.
    /// </summary>
    [DataField]
    public float BloodAmount = 25f;

    /// <summary>
    /// Количество разбрызгиваемых кровавых клякс вокруг.
    /// </summary>
    [DataField]
    public int SplatterCount = 4;
}
