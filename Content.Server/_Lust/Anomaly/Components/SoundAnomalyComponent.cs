using Content.Server._Lust.Anomaly.Systems;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.Anomaly.Components;

/// <summary>
/// Запись услышанной речи с привязкой к голосу говорящего.
/// </summary>
[DataDefinition]
public sealed partial class HeardSpeechRecord
{
    [DataField]
    public string Message = string.Empty;

    [DataField]
    public string? VoiceId;
}

/// <summary>
/// Звуковая аномалия (Sound Anomaly): глушит связь, издает фантомные звуки, эхолалит услышанную речь с копированием голоса (TTS) или голосом SCP-049, взрывает лампы и стекла, а также наводит слуховые галлюцинации в голову.
/// </summary>
[RegisterComponent, Access(typeof(SoundAnomalySystem))]
public sealed partial class SoundAnomalyComponent : Component
{
    /// <summary>
    /// Базовый радиус звукового импульса при обычном пульсе.
    /// </summary>
    [DataField]
    public float PulseRadius = 8f;

    /// <summary>
    /// Базовый радиус разрушительного звукового удара при суперкритике.
    /// </summary>
    [DataField]
    public float SupercriticalRadius = 16f;

    /// <summary>
    /// Сила отталкивания при суперкритике.
    /// </summary>
    [DataField]
    public float SupercriticalPushStrength = 24f;

    /// <summary>
    /// Длительность оглушения и нокдауна при суперкритике.
    /// </summary>
    [DataField]
    public TimeSpan SupercriticalStunDuration = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Шанс проигрывания фантомного звука случайному наблюдателю поблизости.
    /// </summary>
    [DataField]
    public float PassivePhantomSoundChance = 0.95f;

    /// <summary>
    /// Время между проверками пассивного звука.
    /// </summary>
    [DataField]
    public TimeSpan PassiveSoundInterval = TimeSpan.FromSeconds(0.8);

    /// <summary>
    /// Внутреннее время следующей проверки пассивного звука.
    /// </summary>
    public TimeSpan NextPassiveSoundTime = TimeSpan.Zero;

    /// <summary>
    /// Огромный разнообразный пул фантомных звуков (оружие, шаги, сирены, электроника, крики, шумы станции).
    /// </summary>
    [DataField]
    public List<SoundSpecifier> PhantomSounds = new()
    {
        // Оружие / затворы / выстрелы / рикошеты
        new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/shotgun_close.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/shotgun_open.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/revolver_cock.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/sf_rifle_cock.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/smg_cock.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Weapons/Guns/Cock/shotgun_cock.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Casings/shotgun_fall.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/shotgun.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/sniper.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/laser.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/laser3.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/laser_cannon.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/laser_cannon2.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/laser_clown.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/magnetic_shot.ogg"),
        new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Weapons/Guns/SMGs/mp5/mp5_shot_1.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Weapons/Guns/SMGs/mp5/mp5_shot_2.ogg"),

        // Шаги и перемещения
        new SoundPathSpecifier("/Audio/Effects/Footsteps/spurs1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/spurs2.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/bells1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/bells3.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/wood1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/wood3.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/clownstep1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/clownstep2.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/clownspiderstep1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/jesterstep1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/snowstep1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/glass_step.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/gib_step.ogg"),

        // Жуткие / биологические / крики
        new SoundPathSpecifier("/Audio/Effects/bone_rattle.ogg"),
        new SoundPathSpecifier("/Audio/Animals/mouse_squeak.ogg"),
        new SoundPathSpecifier("/Audio/Animals/monkey_scream.ogg"),
        new SoundPathSpecifier("/Audio/Machines/robotscream_1.ogg"),
        new SoundPathSpecifier("/Audio/Machines/robotscream_2.ogg"),
        new SoundPathSpecifier("/Audio/Machines/robotscream_5.ogg"),

        // Стоны и вздохи
        new SoundPathSpecifier("/Audio/_Lust/Interactions/femston1.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Interactions/femston2.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Interactions/femston3.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Interactions/femston4.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Interactions/femston5.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Interactions/femston6.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Interactions/femston7.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Interactions/maleston1.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Interactions/maleston2.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Interactions/euphmoan1.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Interactions/euphmoan2.ogg"),
        new SoundPathSpecifier("/Audio/_Lust/Interactions/euphmoan3.ogg"),

        // Стекло, удары, треск
        new SoundPathSpecifier("/Audio/Effects/glass_knock.ogg"),
        new SoundPathSpecifier("/Audio/Effects/glass_crack1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/glass_crack2.ogg"),
        new SoundPathSpecifier("/Audio/Effects/metal_thud1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/metal_crunch.ogg"),
        new SoundPathSpecifier("/Audio/Effects/balloon-pop.ogg"),
        new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"),
        new SoundPathSpecifier("/Audio/Effects/beep1.ogg"),

        // Шлюзы, болты, механизмы
        new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg"),
        new SoundPathSpecifier("/Audio/Machines/airlock_close.ogg"),
        new SoundPathSpecifier("/Audio/Machines/boltsdown.ogg"),
        new SoundPathSpecifier("/Audio/Machines/boltsup.ogg"),

        // Машины, сирены, звонки
        new SoundPathSpecifier("/Audio/Machines/alarm.ogg"),
        new SoundPathSpecifier("/Audio/Machines/warning_buzzer_xenoborg.ogg"),
        new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg"),
        new SoundPathSpecifier("/Audio/Machines/double_ring.ogg"),
        new SoundPathSpecifier("/Audio/Machines/sonar-ping.ogg"),
        new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg"),
        new SoundPathSpecifier("/Audio/Machines/twobeep.ogg"),
        new SoundPathSpecifier("/Audio/Machines/ding.ogg"),
        new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg"),
        new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg"),
        new SoundPathSpecifier("/Audio/Effects/countdown.ogg"),
        new SoundPathSpecifier("/Audio/Effects/church_bell2.ogg"),
        new SoundPathSpecifier("/Audio/Effects/church_bell4.ogg"),

        // Электричество, искры, всплески
        new SoundPathSpecifier("/Audio/Effects/sparks1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/sparks2.ogg"),
        new SoundPathSpecifier("/Audio/Effects/sparks3.ogg"),
        new SoundPathSpecifier("/Audio/Effects/sparks4.ogg"),
        new SoundPathSpecifier("/Audio/Effects/radpulse1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/radpulse3.ogg"),
        new SoundPathSpecifier("/Audio/Effects/radpulse7.ogg"),
        new SoundPathSpecifier("/Audio/Effects/zzzt.ogg"),

        // Предметы, клаксоны, инструменты
        new SoundPathSpecifier("/Audio/Items/airhorn.ogg"),
        new SoundPathSpecifier("/Audio/Items/bikehorn.ogg"),
        new SoundPathSpecifier("/Audio/Items/brokenbikehorn.ogg"),
        new SoundPathSpecifier("/Audio/Items/desk_bell_ring.ogg"),
        new SoundPathSpecifier("/Audio/Items/Whistle/whistle_5.ogg"),
        new SoundPathSpecifier("/Audio/Items/Paper/paper_scribble5.ogg"),
        new SoundPathSpecifier("/Audio/Items/welder.ogg"),
        new SoundPathSpecifier("/Audio/Items/welder2.ogg"),
        new SoundPathSpecifier("/Audio/Items/crowbar.ogg"),
        new SoundPathSpecifier("/Audio/Items/hypospray.ogg"),
        new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_start.ogg"),
        new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_end.ogg"),
        new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_breakout_start.ogg"),
        new SoundPathSpecifier("/Audio/Items/Toys/pushHornHonk.ogg"),

        // Атмосфера, эмбиент
        new SoundPathSpecifier("/Audio/Ambience/Objects/emf_buzz.ogg"),
        new SoundPathSpecifier("/Audio/Ambience/Objects/gravity_gen_hum.ogg")
    };

    /// <summary>
    /// Звук резкого оглушения / звона в ушах при звуковом ударе.
    /// </summary>
    [DataField]
    public SoundSpecifier EarRingingSound = new SoundPathSpecifier("/Audio/_Sunrise/flashbang.ogg");

    /// <summary>
    /// Звук звукового удара при суперкритике.
    /// </summary>
    [DataField]
    public SoundSpecifier SupercriticalSound = new SoundPathSpecifier("/Audio/Effects/explosionfar.ogg");

    /// <summary>
    /// Звук при импульсе аномалии.
    /// </summary>
    [DataField]
    public SoundSpecifier PulseSound = new SoundPathSpecifier("/Audio/Ambience/Objects/gravity_gen_hum.ogg");

    /// <summary>
    /// Звук пространственного эха при эхолалии.
    /// </summary>
    [DataField]
    public SoundSpecifier EchoSound = new SoundPathSpecifier("/Audio/Machines/sonar-ping.ogg");

    /// <summary>
    /// Интервал между выкриками эхолалии.
    /// </summary>
    [DataField]
    public TimeSpan EchoInterval = TimeSpan.FromSeconds(7);

    /// <summary>
    /// Время следующего выкрика эхолалии.
    /// </summary>
    public TimeSpan NextEchoTime = TimeSpan.Zero;

    /// <summary>
    /// Шанс того, что аномалия воспроизведет эхо при срабатывании таймера.
    /// </summary>
    [DataField]
    public float EchoChance = 0.85f;

    /// <summary>
    /// Максимальное число запоминаемых фраз для повторения.
    /// </summary>
    [DataField]
    public int MaxHeardPhrases = 15;

    /// <summary>
    /// Запомненные записи речи вместе с голосами.
    /// </summary>
    public List<HeardSpeechRecord> HeardRecords = new();

    /// <summary>
    /// Последний запомненный голос реального игрока (не SCP049).
    /// </summary>
    public string? LastPlayerVoiceId;

    /// <summary>
    /// Голос по умолчанию для речи аномалии (SCP-049).
    /// </summary>
    [DataField]
    public ProtoId<TTSVoicePrototype> DefaultVoice = "SCP049";

    /// <summary>
    /// Фразы по умолчанию, если аномалия еще ничего не услышала от игроков.
    /// </summary>
    [DataField]
    public List<string> DefaultEchoPhrases = new()
    {
        "...кто здесь?..",
        "...тишина...",
        "...я слышу тебя...",
        "...звук... волны...",
        "...оно дышит...",
        "...уходи отсюда..."
    };

    /// <summary>
    /// Шанс взрыва лампочек в радиусе импульса.
    /// </summary>
    [DataField]
    public float LightBreakChance = 0.7f;

    /// <summary>
    /// Шанс непроизвольного крика гуманоида от боли в ушах при импульсе.
    /// </summary>
    [DataField]
    public float ScreamChance = 0.85f;

    /// <summary>
    /// Структурный урон стеклам и посуде при обычном импульсе.
    /// </summary>
    [DataField]
    public DamageSpecifier PulseWindowDamage = new();

    /// <summary>
    /// Структурный урон, наносимый стеклам и окнам при суперкритике.
    /// </summary>
    [DataField]
    public DamageSpecifier WindowDamage = new();

    /// <summary>
    /// Время последнего услышанного громкого звука или речи рядом с аномалией.
    /// </summary>
    public TimeSpan LastSoundActivityTime = TimeSpan.Zero;

    /// <summary>
    /// Порог тишины (в секундах). Если вокруг тихо дольше этого времени, аномалия переходит в режим сна/успокоения.
    /// </summary>
    [DataField]
    public TimeSpan SilenceThreshold = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Находится ли аномалия в состоянии глубокой тишины (засыпает).
    /// </summary>
    public bool IsDormant = false;

    /// <summary>
    /// Скорость снижения нестабильности в секунду в полной тишине.
    /// </summary>
    [DataField]
    public float SilenceStabilityDecayRate = 0.015f;

    /// <summary>
    /// Радиус обнаружения бегущих персонажей для пробуждения аномалии.
    /// </summary>
    [DataField]
    public float RunningDetectionRadius = 4f;

    /// <summary>
    /// Время последней проверки и возбуждения от бега персонажей поблизости.
    /// </summary>
    public TimeSpan LastRunningAgitationTime = TimeSpan.Zero;
}
