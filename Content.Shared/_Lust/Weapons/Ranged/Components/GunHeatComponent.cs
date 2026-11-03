using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true, raiseAfterAutoHandleState: true)]
public sealed partial class GunHeatComponent : Component
{
    /// <summary>
    /// Тепло, добавляемое при каждом выстреле.
    /// </summary>
    [DataField]
    public float HeatPerShot = 750f;

    /// <summary>
    /// Изменение температуры на один выстрел с учетом теплоемкости.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float TemperaturePerShot;

    /// <summary>
    /// Скорость пассивного охлаждения оружия.
    /// </summary>
    [DataField]
    public float PassiveCooling = 2.5f;

    /// <summary>
    /// Температура, при которой появляется визуальное свечение.
    /// </summary>
    [DataField]
    public float GlowTemperature = Atmospherics.T20C + 50f;

    /// <summary>
    /// Температура, при которой оружие может заклинить.
    /// </summary>
    [DataField]
    public float JamTemperature = Atmospherics.T20C + 160f;

    /// <summary>
    /// Максимальная вероятность заклинивания при критическом нагреве.
    /// </summary>
    [DataField]
    public float MaxJamChance = 0.35f;

    /// <summary>
    /// Температура плавления оружия.
    /// </summary>
    [DataField]
    public float MeltTemperature = Atmospherics.T20C + 360f;

    /// <summary>
    /// Текущая температура оружия.
    /// </summary>
    [AutoNetworkedField]
    public float Temperature = Atmospherics.T20C;

    /// <summary>
    /// Указывает, что оружие заклинило.
    /// </summary>
    [AutoNetworkedField]
    public bool Jammed;

    /// <summary>
    /// Количество выстрелов, сделанных оружием.
    /// </summary>
    [AutoNetworkedField]
    public int ShotCount;

    /// <summary>
    /// Звук заклинивания оружия.
    /// </summary>
    [DataField]
    public SoundSpecifier JamSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Empty/empty.ogg");

    /// <summary>
    /// Звук снятия оружия с заклинивания.
    /// </summary>
    [DataField]
    public SoundSpecifier UnjamSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/smg_cock.ogg");

    /// <summary>
    /// Интервал между всплывающими сообщениями о заклинивании.
    /// </summary>
    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Время, после которого можно снова показать сообщение о заклинивании.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan NextPopupTime;
}
