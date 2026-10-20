using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Smoking;

/// <summary>
/// Компонент для электронного испарителя (вейпа).
/// Использует сменные картриджи с жидкостью через ItemSlots.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class VapeComponent : Component
{
    /// <summary>
    /// Время затяжки при использовании на других персонажах.
    /// </summary>
    [DataField]
    public float Delay = 2.5f;

    /// <summary>
    /// Время затяжки при парении на себе.
    /// </summary>
    [DataField]
    public float UserDelay = 1.2f;

    /// <summary>
    /// Минимальный интервал между затяжками.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Время последней затяжки вейпом. Учитывает паузу сервера/энтити.
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan LastPuffTime = TimeSpan.Zero;

    /// <summary>
    /// Мощность взрыва при взрыве вейпа.
    /// </summary>
    [DataField]
    public float ExplosionIntensity = 2.5f;

    /// <summary>
    /// Взрываться ли при следующей затяжке (например, после взлома/емага).
    /// </summary>
    [DataField]
    public bool ExplodeOnUse = false;

    /// <summary>
    /// Урон, наносимый персонажу при затяжке (если задан).
    /// </summary>
    [DataField]
    public DamageSpecifier? Damage;

    /// <summary>
    /// Тип газа, выделяемого в атмосферу при затяжке.
    /// </summary>
    [DataField]
    public Gas GasType = Gas.WaterVapor;

    /// <summary>
    /// Список взрывоопасных реагентов при нагревании спирали.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ReagentPrototype>> ExplosiveReagents = new()
    {
        "Plasma",
        "Tritium"
    };

    /// <summary>
    /// Название слота ItemSlots для вставки картриджа.
    /// </summary>
    [DataField]
    public string CartridgeSlot = "cartridge";

    /// <summary>
    /// Объем жидкости, выпариваемый за одну затяжку.
    /// </summary>
    [DataField]
    public float PuffVolume = 1.0f;

    /// <summary>
    /// Количество молей пара, выделяемых в атмосферу за затяжку.
    /// </summary>
    [DataField]
    public float VaporMolesPerPuff = 2.0f;

    /// <summary>
    /// Звук нагрева спирали/затяжки вейпом.
    /// </summary>
    [DataField]
    public SoundSpecifier? PuffSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/vape_puff.ogg");

    /// <summary>
    /// Звук выдоха пара.
    /// </summary>
    [DataField]
    public SoundSpecifier? ExhaleSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/cigarette_exhale.ogg");

    /// <summary>
    /// Прототип действия запуска колечка пара.
    /// </summary>
    [DataField]
    public EntProtoId SmokeRingAction = "ActionBlowSmokeRing";

    /// <summary>
    /// Сущность действия выдувания колечка пара.
    /// </summary>
    public EntityUid? SmokeRingActionEntity;
}
