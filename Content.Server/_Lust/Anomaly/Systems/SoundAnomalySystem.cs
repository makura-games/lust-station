using System.Text;
using Content.Server._Lust.Anomaly.Components;
using Content.Server._Sunrise.TTS;
using Content.Server.Anomaly;
using Content.Server.Atmos.Components;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Flash;
using Content.Server.GameTicking;
using Content.Server.Light.EntitySystems;
using Content.Server.Radio;
using Content.Server.Speech.EntitySystems;
using Content.Shared._Lust.Anomaly.Components;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Drunk;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Jittering;
using Content.Shared.Light.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Lust.Anomaly.Systems;

/// <summary>
/// Серверная система для управления звуковой аномалией: глушение радиосвязи,
/// эхолалия услышанной речи через TTS, повреждение стекол и ламп, слуховые галлюцинации и ядра.
/// </summary>
public sealed partial class SoundAnomalySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedDrunkSystem _drunk = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private FlashSystem _flash = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private StutteringSystem _stutter = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private PoweredLightSystem _poweredLight = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private TTSSystem _tts = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private AnomalySystem _anomaly = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private static readonly ProtoId<TagPrototype> WindowTag = "Window";
    private static readonly ProtoId<TagPrototype> DrinkBottleTag = "DrinkBottle";
    private static readonly ProtoId<TagPrototype> GlassBeakerTag = "GlassBeaker";
    private static readonly ProtoId<TagPrototype> GlassAirlockTag = "GlassAirlock";
    private static readonly ProtoId<TagPrototype> HelmetEvaTag = "HelmetEVA";
    private static readonly ProtoId<TagPrototype> SecurityHelmetTag = "SecurityHelmet";

    private static readonly List<SoundSpecifier> ResonanceStepSounds = new()
    {
        new SoundPathSpecifier("/Audio/Effects/Footsteps/spurs1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/spurs2.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/bells1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/bells3.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/glass_step.ogg"),
        new SoundPathSpecifier("/Audio/Effects/glass_knock.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Footsteps/clownstep1.ogg")
    };

    /// <summary>
    /// Последние сохраненные фразы от аномалий для ядер.
    /// </summary>
    private List<HeardSpeechRecord> _latestAnomalyRecords = new();

    /// <summary>
    /// Время, до которого действует глобальное глушение радиосвязи на всей станции (эффект солнечной вспышки после суперкритики).
    /// </summary>
    private TimeSpan _globalRadioBlackoutUntil = TimeSpan.Zero;

    /// <summary>
    /// Аккумулятор времени для периодического обновления логики аномалии вместо выполнения каждый кадр.
    /// </summary>
    private float _updateAccumulator;

    private const float UpdateInterval = 0.5f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<AcousticProtectionComponent, ExaminedEvent>(OnAcousticProtectionExamined);
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);

        SubscribeLocalEvent<SoundAnomalyComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<SoundAnomalyComponent, AnomalyShutdownEvent>(OnShutdown);
        SubscribeLocalEvent<SoundAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<SoundAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);

        SubscribeLocalEvent<SoundAnomalyHauntedComponent, ComponentShutdown>(OnHauntedShutdown);
        SubscribeLocalEvent<SoundAnomalyCoreComponent, UseInHandEvent>(OnCoreUseInHand);
        SubscribeLocalEvent<SoundAnomalyResonanceComponent, MoveEvent>(OnResonanceMove);
    }

    #region Event Handlers

    /// <summary>
    /// Очистка состояния системы при перезапуске раунда.
    /// </summary>
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _latestAnomalyRecords.Clear();
        _globalRadioBlackoutUntil = TimeSpan.Zero;
        _updateAccumulator = 0f;
    }

    /// <summary>
    /// Отображение информации об акустической защите при осмотре предмета.
    /// </summary>
    private void OnAcousticProtectionExamined(Entity<AcousticProtectionComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("acoustic-protection-examine"));
    }

    /// <summary>
    /// Проверка возможности приема радиосообщения (учитывает перегрузку и глобальное глушение).
    /// </summary>
    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (_timing.CurTime < _globalRadioBlackoutUntil)
        {
            args.Cancelled = true;
            return;
        }

        if (HasComp<SoundAnomalyOverloadedRadioComponent>(args.RadioReceiver) ||
            HasComp<SoundAnomalyOverloadedRadioComponent>(args.RadioSource))
        {
            args.Cancelled = true;
        }
    }

    /// <summary>
    /// Проверка возможности отправки радиосообщения (учитывает перегрузку и глобальное глушение).
    /// </summary>
    private void OnRadioSendAttempt(ref RadioSendAttemptEvent args)
    {
        if (_timing.CurTime < _globalRadioBlackoutUntil)
        {
            args.Cancelled = true;
            return;
        }

        if (HasComp<SoundAnomalyOverloadedRadioComponent>(args.RadioSource))
        {
            args.Cancelled = true;
        }
    }

    /// <summary>
    /// Обработка услышанной аномалией речи персонажей поблизости.
    /// </summary>
    private void OnListen(Entity<SoundAnomalyComponent> ent, ref ListenEvent args)
    {
        if (args.Source == ent.Owner || string.IsNullOrWhiteSpace(args.Message))
            return;

        var comp = ent.Comp;
        var curTime = _timing.CurTime;

        comp.LastSoundActivityTime = curTime;
        if (comp.IsDormant)
        {
            comp.IsDormant = false;
            _popup.PopupEntity(Loc.GetString("sound-anomaly-agitated-sound"), ent.Owner, PopupType.MediumCaution);
        }

        if (TryComp<AnomalyComponent>(ent.Owner, out var anom))
        {
            _anomaly.ChangeAnomalyStability(ent.Owner, 0.05f, anom);
        }

        string? speakerVoice = null;
        if (TryComp<TTSComponent>(args.Source, out var tts) && tts.VoicePrototypeId != null)
        {
            speakerVoice = tts.VoicePrototypeId.Value;
            if (speakerVoice != "SCP049")
            {
                comp.LastPlayerVoiceId = speakerVoice;
            }
        }

        if (comp.HeardRecords.Count >= comp.MaxHeardPhrases)
            comp.HeardRecords.RemoveAt(0);

        comp.HeardRecords.Add(new HeardSpeechRecord
        {
            Message = args.Message.Trim(),
            VoiceId = speakerVoice
        });

        _latestAnomalyRecords = new(comp.HeardRecords);
    }

    /// <summary>
    /// Сохранение последних записей речи при деспавне аномалии.
    /// </summary>
    private void OnShutdown(Entity<SoundAnomalyComponent> ent, ref AnomalyShutdownEvent args)
    {
        if (ent.Comp.HeardRecords.Count > 0)
        {
            _latestAnomalyRecords = new(ent.Comp.HeardRecords);
        }
    }

    /// <summary>
    /// Гарантированное восстановление оригинального голоса при удалении компонента галлюцинаций.
    /// </summary>
    private void OnHauntedShutdown(Entity<SoundAnomalyHauntedComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.OriginalVoiceId != null && TryComp<TTSComponent>(ent.Owner, out var tts))
        {
            tts.VoicePrototypeId = ent.Comp.OriginalVoiceId;
            ent.Comp.OriginalVoiceId = null;
        }
    }

    /// <summary>
    /// Обработчик использования ядра звуковой аномалии в руках.
    /// </summary>
    private void OnCoreUseInHand(Entity<SoundAnomalyCoreComponent> coreEnt, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryUseCore(coreEnt, args.User))
            args.Handled = true;
    }

    /// <summary>
    /// Проигрывание резонансных звуков шагов при передвижении зараженной сущности.
    /// </summary>
    private void OnResonanceMove(Entity<SoundAnomalyResonanceComponent> ent, ref MoveEvent ev)
    {
        if (ev.OldPosition.EntityId == ev.NewPosition.EntityId &&
            (ev.OldPosition.Position - ev.NewPosition.Position).LengthSquared() < 0.001f)
            return;

        var curTime = _timing.CurTime;
        if (curTime - ent.Comp.LastStepTime < TimeSpan.FromSeconds(0.35))
            return;

        ent.Comp.LastStepTime = curTime;
        _audio.PlayPvs(_random.Pick(ResonanceStepSounds), ent.Owner, AudioParams.Default.WithVolume(2.5f).WithVariation(0.15f));
    }

    /// <summary>
    /// Обработчик регулярного звукового импульса аномалии.
    /// </summary>
    private void OnPulse(Entity<SoundAnomalyComponent> ent, ref AnomalyPulseEvent args)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;
        var range = comp.PulseRadius * args.Stability;

        _audio.PlayPvs(comp.PulseSound, uid);

        var radioEntities = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic | LookupFlags.Sundries);
        foreach (var radioEnt in radioEntities)
        {
            if (IsRadioDevice(radioEnt))
            {
                var overloaded = EnsureComp<SoundAnomalyOverloadedRadioComponent>(radioEnt);
                overloaded.OverloadedUntil = _timing.CurTime + TimeSpan.FromSeconds(15 + 15 * args.Stability);
            }
        }

        var glassEntities = _lookup.GetEntitiesInRange(uid, range * 0.7f, LookupFlags.Dynamic | LookupFlags.Static);
        foreach (var glassEnt in glassEntities)
        {
            if (IsGlassObject(glassEnt) && comp.PulseWindowDamage.GetTotal() > 0)
            {
                _damageable.TryChangeDamage(glassEnt, comp.PulseWindowDamage, ignoreResistances: true, origin: uid);
            }
        }

        var lights = _lookup.GetEntitiesInRange<PoweredLightComponent>(Transform(uid).Coordinates, range);
        foreach (var light in lights)
        {
            if (_random.Prob(comp.LightBreakChance * args.Stability))
            {
                _poweredLight.TryDestroyBulb(light.Owner, light.Comp);
            }
        }

        var mobs = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic);
        foreach (var target in mobs)
        {
            if (target == uid)
                continue;

            if (!HasComp<MobStateComponent>(target) && !HasComp<ActorComponent>(target))
                continue;

            var severity = args.Stability;

            var protectedEars = HasAcousticProtection(target);
            if (protectedEars)
            {
                _stamina.TakeStaminaDamage(target, 20f * severity);
                _popup.PopupEntity(Loc.GetString("sound-anomaly-protected-ears"), target, target, PopupType.SmallCaution);
            }
            else
            {
                var stunTime = TimeSpan.FromSeconds(1.5 * severity);
                var knockdownTime = TimeSpan.FromSeconds(2.0 * severity);
                var jitterTime = TimeSpan.FromSeconds(4.0 * severity);
                var drunkTime = TimeSpan.FromSeconds(8.0 * severity);
                var stutterTime = TimeSpan.FromSeconds(10.0 * severity);
                var flashTime = TimeSpan.FromMilliseconds(2000 * severity);

                _stun.TryKnockdown(target, knockdownTime, force: true);
                _stun.TryAddStunDuration(target, stunTime);
                _stamina.TakeStaminaDamage(target, 45f + 30f * severity);

                _jitter.DoJitter(target, jitterTime, true);
                _drunk.TryApplyDrunkenness(target, drunkTime);
                _stutter.DoStutter(target, stutterTime, true);

                _flash.Flash(target, null, null, flashTime, 0.5f);

                var haunted = EnsureComp<SoundAnomalyHauntedComponent>(target);
                haunted.SourceAnomaly = uid;
                haunted.SavedRecords = new(comp.HeardRecords);
                haunted.EndTime = _timing.CurTime + TimeSpan.FromSeconds(40 + 20 * severity);
                haunted.NextHallucinationTime = _timing.CurTime + TimeSpan.FromSeconds(1f);

                if (comp.LastPlayerVoiceId != null && TryComp<TTSComponent>(target, out var targetTts))
                {
                    if (haunted.OriginalVoiceId == null)
                    {
                        haunted.OriginalVoiceId = targetTts.VoicePrototypeId;
                    }
                    targetTts.VoicePrototypeId = comp.LastPlayerVoiceId;
                    haunted.VoiceResetTime = _timing.CurTime + TimeSpan.FromMinutes(5);
                    _popup.PopupEntity(Loc.GetString("sound-anomaly-voice-distorted"), target, target, PopupType.LargeCaution);
                }

                if (_random.Prob(comp.ScreamChance))
                {
                    _chat.TryEmoteWithChat(target, "Scream", ignoreActionBlocker: true);
                }

                _audio.PlayPvs(comp.EarRingingSound, target, AudioParams.Default.WithVolume(10f));
                _popup.PopupEntity(Loc.GetString("sound-anomaly-ringing-ears"), target, PopupType.LargeCaution);
            }
        }
    }

    /// <summary>
    /// Обработчик сверхкритического состояния звуковой аномалии.
    /// </summary>
    private void OnSupercritical(Entity<SoundAnomalyComponent> ent, ref AnomalySupercriticalEvent args)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;
        var range = comp.SupercriticalRadius * args.PowerModifier;
        var strength = comp.SupercriticalPushStrength * args.PowerModifier;
        var worldPos = _xform.GetWorldPosition(uid);

        _globalRadioBlackoutUntil = _timing.CurTime + TimeSpan.FromMinutes(5);
        _audio.PlayPvs(comp.SupercriticalSound, uid);

        var entities = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic | LookupFlags.Static);
        foreach (var target in entities)
        {
            if (target == uid)
                continue;

            if (TryComp<PoweredLightComponent>(target, out var light))
            {
                _poweredLight.TryDestroyBulb(target, light);
            }

            if (IsGlassObject(target))
            {
                if (comp.WindowDamage.GetTotal() > 0)
                {
                    _damageable.TryChangeDamage(target, comp.WindowDamage, ignoreResistances: true, origin: uid);
                }
            }

            var diff = _xform.GetWorldPosition(target) - worldPos;
            if (diff.LengthSquared() > 0.001f)
            {
                _throwing.TryThrow(target, diff.Normalized() * 8f, strength, uid, 0);
            }

            if (HasComp<MobStateComponent>(target) || HasComp<ActorComponent>(target))
            {
                var protectedEars = HasAcousticProtection(target);
                if (protectedEars)
                {
                    _stamina.TakeStaminaDamage(target, 35f);
                    _popup.PopupEntity(Loc.GetString("sound-anomaly-protected-ears"), target, target, PopupType.MediumCaution);
                }
                else
                {
                    _stun.TryKnockdown(target, comp.SupercriticalStunDuration, force: true);
                    _stun.TryAddStunDuration(target, comp.SupercriticalStunDuration);
                    _stamina.TakeStaminaDamage(target, 100f);

                    _jitter.DoJitter(target, TimeSpan.FromSeconds(12), true);
                    _drunk.TryApplyDrunkenness(target, TimeSpan.FromSeconds(25));
                    _stutter.DoStutter(target, TimeSpan.FromSeconds(30), true);

                    _flash.Flash(target, null, null, TimeSpan.FromMilliseconds(5000), 0.8f);
                    _chat.TryEmoteWithChat(target, "Scream", ignoreActionBlocker: true);

                    var distToTarget = (_xform.GetWorldPosition(target) - worldPos).Length();
                    if (distToTarget <= 10f)
                    {
                        var permHaunted = EnsureComp<SoundAnomalyHauntedComponent>(target);
                        permHaunted.SourceAnomaly = uid;
                        permHaunted.SavedRecords = new(comp.HeardRecords);
                        permHaunted.EndTime = TimeSpan.MaxValue;
                        permHaunted.NextHallucinationTime = _timing.CurTime + TimeSpan.FromSeconds(1f);
                        _popup.PopupEntity(Loc.GetString("sound-anomaly-permanent-whisper"), target, target, PopupType.LargeCaution);
                    }
                    else
                    {
                        var haunted = EnsureComp<SoundAnomalyHauntedComponent>(target);
                        haunted.SourceAnomaly = uid;
                        haunted.SavedRecords = new(comp.HeardRecords);
                        haunted.EndTime = _timing.CurTime + TimeSpan.FromSeconds(90);
                        haunted.NextHallucinationTime = _timing.CurTime + TimeSpan.FromSeconds(1f);
                    }

                    _audio.PlayPvs(comp.EarRingingSound, target, AudioParams.Default.WithVolume(15f));
                    _popup.PopupEntity(Loc.GetString("sound-anomaly-eardrum-rupture"), target, PopupType.LargeCaution);
                }
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Проверяет наличие средств защиты слуха на сущности (включая надетое снаряжение).
    /// </summary>
    public bool HasAcousticProtection(EntityUid uid)
    {
        if (HasComp<AcousticProtectionComponent>(uid))
            return true;

        if (!TryComp<InventoryComponent>(uid, out var inv))
            return false;

        foreach (var slot in new[] { "ears", "head" })
        {
            if (_inventory.TryGetSlotEntity(uid, slot, out var item, inv))
            {
                if (HasComp<AcousticProtectionComponent>(item.Value))
                    return true;

                if (HasComp<PressureProtectionComponent>(item.Value))
                    return true;

                if (_tag.HasTag(item.Value, HelmetEvaTag) || _tag.HasTag(item.Value, SecurityHelmetTag))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Попытка использования ядра звуковой аномалии (паттерн OnEvent -> TryDo -> CanDo -> Do).
    /// </summary>
    public bool TryUseCore(Entity<SoundAnomalyCoreComponent> coreEnt, EntityUid user)
    {
        if (!CanUseCore(coreEnt, user))
            return false;

        DoUseCore(coreEnt, user);
        return true;
    }

    /// <summary>
    /// Проверка возможности использования ядра звуковой аномалии без побочных эффектов.
    /// </summary>
    public bool CanUseCore(Entity<SoundAnomalyCoreComponent> coreEnt, EntityUid user, bool quiet = false)
    {
        if (_timing.CurTime < coreEnt.Comp.NextUseTime)
            return false;

        return true;
    }

    #endregion

    #region Overrides

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateAccumulator += frameTime;
        if (_updateAccumulator < UpdateInterval)
            return;

        var elapsed = _updateAccumulator;
        _updateAccumulator = 0f;

        var curTime = _timing.CurTime;

        // Очистка истекших перегруженных радиоустройств
        var radioQuery = EntityQueryEnumerator<SoundAnomalyOverloadedRadioComponent>();
        while (radioQuery.MoveNext(out var radioEnt, out var over))
        {
            if (curTime >= over.OverloadedUntil)
            {
                RemCompDeferred<SoundAnomalyOverloadedRadioComponent>(radioEnt);
            }
        }

        var anomalyQuery = EntityQueryEnumerator<SoundAnomalyComponent, AnomalyComponent, TransformComponent>();
        var activeAnomalyPositions = new List<(EntityCoordinates Coords, float Radius)>();

        while (anomalyQuery.MoveNext(out var uid, out var sound, out var anom, out var xform))
        {
            var range = sound.PulseRadius * 2.0f;
            activeAnomalyPositions.Add((xform.Coordinates, sound.PulseRadius));

            var entities = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic);
            var hasAudience = false;
            foreach (var ent in entities)
            {
                if (HasComp<ActorComponent>(ent) || HasComp<MobStateComponent>(ent))
                {
                    hasAudience = true;
                    break;
                }
            }

            // Проверка быстрого бега персонажей в пределах RunningDetectionRadius
            var nearbyEntities = _lookup.GetEntitiesInRange(uid, sound.RunningDetectionRadius, LookupFlags.Dynamic);
            foreach (var nearEnt in nearbyEntities)
            {
                if (TryComp<PhysicsComponent>(nearEnt, out var physics) &&
                    TryComp<MovementSpeedModifierComponent>(nearEnt, out var move))
                {
                    // Бег считается только если скорость заметно превышает нормальную скорость ходьбы
                    if (physics.LinearVelocity.Length() > move.CurrentWalkSpeed + 0.6f)
                    {
                        sound.LastSoundActivityTime = curTime;
                        if (curTime - sound.LastRunningAgitationTime > TimeSpan.FromSeconds(3))
                        {
                            sound.LastRunningAgitationTime = curTime;
                            if (sound.IsDormant)
                            {
                                sound.IsDormant = false;
                                _popup.PopupEntity(Loc.GetString("sound-anomaly-agitated-running"), uid, PopupType.MediumCaution);
                            }
                            _anomaly.ChangeAnomalyStability(uid, 0.02f, anom);
                        }
                        break;
                    }
                }
            }

            // Режим сна в тишине
            if (curTime - sound.LastSoundActivityTime > sound.SilenceThreshold)
            {
                if (!sound.IsDormant)
                {
                    sound.IsDormant = true;
                    _popup.PopupEntity(Loc.GetString("sound-anomaly-dormant"), uid, PopupType.Small);
                }

                _anomaly.ChangeAnomalyStability(uid, -sound.SilenceStabilityDecayRate * elapsed, anom);
            }
            else
            {
                sound.IsDormant = false;
            }

            // 1. Пассивное проигрывание пугающих фантомных звуков
            if (hasAudience && curTime >= sound.NextPassiveSoundTime)
            {
                sound.NextPassiveSoundTime = curTime + sound.PassiveSoundInterval;

                if (_random.Prob(sound.PassivePhantomSoundChance) && sound.PhantomSounds.Count > 0)
                {
                    var soundToPlay = _random.Pick(sound.PhantomSounds);

                    var audienceList = new List<EntityUid>();
                    foreach (var ent in entities)
                    {
                        if (HasComp<ActorComponent>(ent) || HasComp<MobStateComponent>(ent))
                            audienceList.Add(ent);
                    }

                    if (audienceList.Count > 0)
                    {
                        var target = _random.Pick(audienceList);
                        var audioParams = AudioParams.Default.WithVolume(_random.NextFloat(2f, 4.5f)).WithVariation(0.15f);

                        if (_random.Prob(0.5f))
                        {
                            _audio.PlayEntity(soundToPlay, target, target, audioParams);
                        }
                        else
                        {
                            var offset = _random.NextVector2(2.5f, 6.0f);
                            var randomCoords = xform.Coordinates.Offset(offset);
                            _audio.PlayPvs(soundToPlay, randomCoords, audioParams);
                        }
                    }
                }
            }

            // 2. Периодическая эхолалия услышанной речи
            if (hasAudience && curTime >= sound.NextEchoTime)
            {
                sound.NextEchoTime = curTime + sound.EchoInterval;

                if (_random.Prob(sound.EchoChance))
                {
                    string chosenMessage;
                    string? chosenVoice;

                    if (sound.HeardRecords.Count > 0)
                    {
                        var rec = _random.Pick(sound.HeardRecords);
                        chosenMessage = rec.Message;
                        chosenVoice = rec.VoiceId ?? sound.DefaultVoice;
                    }
                    else if (sound.DefaultEchoPhrases.Count > 0)
                    {
                        chosenMessage = _random.Pick(sound.DefaultEchoPhrases);
                        chosenVoice = sound.DefaultVoice;
                    }
                    else
                    {
                        chosenMessage = "...";
                        chosenVoice = sound.DefaultVoice;
                    }

                    chosenMessage = DistortMessage(chosenMessage);

                    var tts = EnsureComp<TTSComponent>(uid);
                    tts.VoicePrototypeId = chosenVoice;

                    _audio.PlayPvs(sound.EchoSound, uid, AudioParams.Default.WithVolume(3.5f).WithVariation(0.1f));
                    _chat.TrySendInGameICMessage(
                        uid,
                        chosenMessage,
                        InGameICChatType.Speak,
                        ChatTransmitRange.Normal,
                        nameOverride: Loc.GetString("sound-anomaly-echo-speaker-name"),
                        checkRadioPrefix: false,
                        ignoreActionBlocker: true);
                }
            }

            // 3. Заражение резонансом шагов существ поблизости
            if (hasAudience)
            {
                foreach (var ent in entities)
                {
                    if (ent == uid)
                        continue;

                    if (HasComp<MobStateComponent>(ent) && !HasComp<SoundAnomalyResonanceComponent>(ent))
                    {
                        EnsureComp<SoundAnomalyResonanceComponent>(ent);
                    }
                }
            }
        }

        // Очистка резонанса шагов у тех, кто покинул радиус всех аномалий
        var resonanceQuery = EntityQueryEnumerator<SoundAnomalyResonanceComponent, TransformComponent>();
        while (resonanceQuery.MoveNext(out var resUid, out var resComp, out var resXform))
        {
            var inAnyRange = false;
            foreach (var (anomalyCoords, radius) in activeAnomalyPositions)
            {
                if (resXform.Coordinates.TryDistance(EntityManager, anomalyCoords, out var dist) && dist <= radius)
                {
                    inAnyRange = true;
                    break;
                }
            }

            if (!inAnyRange)
            {
                RemCompDeferred<SoundAnomalyResonanceComponent>(resUid);
            }
        }

        // 4. Обработка слуховых галлюцинаций в голове у контуженных живых игроков
        var hauntedQuery = EntityQueryEnumerator<SoundAnomalyHauntedComponent, TransformComponent>();
        while (hauntedQuery.MoveNext(out var victim, out var haunted, out var xform))
        {
            if (TerminatingOrDeleted(victim))
                continue;

            // Возврат исходного голоса жертве по истечении времени сброса
            if (haunted.OriginalVoiceId != null && curTime >= haunted.VoiceResetTime)
            {
                if (TryComp<TTSComponent>(victim, out var tts))
                {
                    tts.VoicePrototypeId = haunted.OriginalVoiceId;
                }
                haunted.OriginalVoiceId = null;
                _popup.PopupEntity(Loc.GetString("sound-anomaly-voice-restored"), victim, victim, PopupType.Medium);
            }

            // Окончание галлюцинаций (если не навсегда)
            if (haunted.EndTime != TimeSpan.MaxValue && curTime > haunted.EndTime)
            {
                if (haunted.OriginalVoiceId != null && TryComp<TTSComponent>(victim, out var tts))
                {
                    tts.VoicePrototypeId = haunted.OriginalVoiceId;
                }
                RemCompDeferred<SoundAnomalyHauntedComponent>(victim);
                continue;
            }

            // Трупы не испытывают слуховых галлюцинаций
            if (TryComp<MobStateComponent>(victim, out var mobState) && mobState.CurrentState != MobState.Alive)
                continue;

            if (curTime < haunted.NextHallucinationTime)
                continue;

            haunted.NextHallucinationTime = curTime + TimeSpan.FromSeconds(_random.NextFloat(
                (float) haunted.MinInterval.TotalSeconds,
                (float) haunted.MaxInterval.TotalSeconds));

            SoundAnomalyComponent? sourceComp = null;
            if (haunted.SourceAnomaly.HasValue && TryComp(haunted.SourceAnomaly.Value, out SoundAnomalyComponent? found))
            {
                sourceComp = found;
            }

            var phantomPool = sourceComp?.PhantomSounds;
            var records = (sourceComp != null && sourceComp.HeardRecords.Count > 0)
                ? sourceComp.HeardRecords
                : haunted.SavedRecords;

            // 50% фантомный звук, 50% шепот фраз
            if (_random.Prob(0.5f) && phantomPool != null && phantomPool.Count > 0)
            {
                var sound = _random.Pick(phantomPool);
                var audioParams = AudioParams.Default.WithVolume(1f).WithVariation(0.1f);

                if (TryComp<ActorComponent>(victim, out var actor))
                {
                    _audio.PlayEntity(sound, victim, victim, audioParams);
                    _popup.PopupEntity(Loc.GetString("sound-anomaly-hallucination-rustle"), victim, victim, PopupType.SmallCaution);
                }
                else
                {
                    _audio.PlayPvs(sound, victim, audioParams.WithMaxDistance(6f));
                }
            }
            else
            {
                string phrase;
                string voice;
                var defaultVoice = sourceComp?.DefaultVoice ?? "SCP049";

                if (records.Count > 0)
                {
                    var rec = _random.Pick(records);
                    phrase = rec.Message;
                    voice = rec.VoiceId ?? defaultVoice;
                }
                else if (sourceComp != null && sourceComp.DefaultEchoPhrases.Count > 0)
                {
                    phrase = _random.Pick(sourceComp.DefaultEchoPhrases);
                    voice = defaultVoice;
                }
                else
                {
                    phrase = Loc.GetString("sound-anomaly-core-default-phrase");
                    voice = defaultVoice;
                }

                if (!string.IsNullOrWhiteSpace(phrase))
                {
                    var whisperPhrase = "... " + phrase.ToLowerInvariant() + " ...";

                    // Персональная отправка шепота исключительно жертве в чат и TTS (без раскрытия окружающим)
                    if (TryComp<ActorComponent>(victim, out var actor))
                    {
                        var wrapped = Loc.GetString("chat-manager-entity-whisper-wrap-message",
                            ("entityName", Loc.GetString("sound-anomaly-whisper-speaker-name")),
                            ("message", whisperPhrase));

                        _chatManager.ChatMessageToOne(
                            ChatChannel.Whisper,
                            whisperPhrase,
                            wrapped,
                            EntityUid.Invalid,
                            false,
                            actor.PlayerSession.Channel);

                        if (_prototypeManager.TryIndex<TTSVoicePrototype>(voice, out var voiceProto))
                        {
                            PlayPersonalHallucinationTts(actor.PlayerSession, phrase, voiceProto, 0.35f);
                        }

                        _popup.PopupEntity(Loc.GetString("sound-anomaly-hallucination-whisper"), victim, victim, PopupType.SmallCaution);
                    }
                }
            }
        }
    }

    #endregion

    #region Helpers & Private Execution

    /// <summary>
    /// Непосредственное выполнение эффекта использования ядра звуковой аномалии.
    /// </summary>
    private void DoUseCore(Entity<SoundAnomalyCoreComponent> coreEnt, EntityUid user)
    {
        var core = coreEnt.Comp;
        var curTime = _timing.CurTime;
        core.NextUseTime = curTime + core.Cooldown;

        if (core.SavedRecords.Count == 0 && _latestAnomalyRecords.Count > 0)
        {
            core.SavedRecords = new(_latestAnomalyRecords);
        }

        string message;
        string voice;

        if (core.SavedRecords.Count > 0)
        {
            var record = _random.Pick(core.SavedRecords);
            message = record.Message;
            voice = record.VoiceId ?? "SCP049";
        }
        else
        {
            message = Loc.GetString("sound-anomaly-core-default-phrase");
            voice = "SCP049";
        }

        if (core.IsInert)
        {
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/sparks1.ogg"), coreEnt.Owner, AudioParams.Default.WithVolume(1f));
            var whisperMsg = "... " + message.ToLowerInvariant() + " ...";
            _chat.TrySendInGameICMessage(
                user,
                whisperMsg,
                InGameICChatType.Whisper,
                ChatTransmitRange.Normal,
                nameOverride: Loc.GetString("sound-anomaly-core-inert-name"),
                checkRadioPrefix: false,
                ignoreActionBlocker: true);

            if (TryComp<ActorComponent>(user, out var actor) &&
                _prototypeManager.TryIndex<TTSVoicePrototype>(voice, out var voiceProto))
            {
                PlayPersonalHallucinationTts(actor.PlayerSession, message, voiceProto, 0.25f);
            }
        }
        else
        {
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/sonar-ping.ogg"), coreEnt.Owner, AudioParams.Default.WithVolume(3f));
            _chat.TrySendInGameICMessage(
                user,
                message,
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                nameOverride: Loc.GetString("sound-anomaly-core-echo-name"),
                checkRadioPrefix: false,
                ignoreActionBlocker: true);

            if (TryComp<ActorComponent>(user, out var actor) &&
                _prototypeManager.TryIndex<TTSVoicePrototype>(voice, out var voiceProto))
            {
                PlayPersonalHallucinationTts(actor.PlayerSession, message, voiceProto, 0.85f);
            }
        }
    }

    /// <summary>
    /// Воспроизведение персонального TTS-звука шепота непосредственно для одного сеанса игрока.
    /// </summary>
    private async void PlayPersonalHallucinationTts(ICommonSession session, string phrase, TTSVoicePrototype voiceProto, float volume = 0.35f)
    {
        if (session.Status != SessionStatus.Connected)
            return;

        if (!_cfg.GetCVar(SunriseCCVars.TTSEnabled))
            return;

        try
        {
            var whisperPhrase = "... " + phrase.ToLowerInvariant() + " ...";
            var soundData = await _tts.GenerateTTS(whisperPhrase, voiceProto);
            if (soundData != null && session.Status == SessionStatus.Connected)
            {
                RaiseNetworkEvent(new PlayTTSEvent(soundData, null, false, volume), session);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to generate personal hallucination TTS: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверяет, является ли сущность приемо-передающим радиоустройством.
    /// </summary>
    private bool IsRadioDevice(EntityUid uid)
    {
        return HasComp<HeadsetComponent>(uid) ||
               HasComp<IntercomComponent>(uid) ||
               HasComp<RadioMicrophoneComponent>(uid) ||
               HasComp<RadioSpeakerComponent>(uid);
    }

    /// <summary>
    /// Проверяет, является ли объект хрупким стеклом или колбой.
    /// </summary>
    private bool IsGlassObject(EntityUid uid)
    {
        if (_tag.HasTag(uid, WindowTag) ||
            _tag.HasTag(uid, DrinkBottleTag) ||
            _tag.HasTag(uid, GlassBeakerTag) ||
            _tag.HasTag(uid, GlassAirlockTag))
        {
            return true;
        }

        if (TryComp<DamageableComponent>(uid, out var damageable) && damageable.DamageModifierSetId != null)
        {
            var mod = damageable.DamageModifierSetId.Value.Id;
            if (mod == "Glass" || mod == "FlimsyGlass")
                return true;
        }

        return false;
    }

    /// <summary>
    /// Искажает фразу, добавляя акустическое эхо, многоточия и повторения слогов.
    /// </summary>
    private string DistortMessage(string original)
    {
        if (string.IsNullOrWhiteSpace(original))
            return original;

        var words = original.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];

            if (i == 0 && _random.Prob(0.4f) && word.Length > 2)
            {
                var repeatPart = word[..Math.Min(2, word.Length)];
                sb.Append(repeatPart).Append('-').Append(repeatPart).Append("... ");
            }

            sb.Append(word);

            if (i < words.Length - 1)
            {
                if (_random.Prob(0.2f))
                    sb.Append("... ");
                else
                    sb.Append(' ');
            }
        }

        if (_random.Prob(0.5f))
            sb.Append("...");

        return sb.ToString();
    }

    #endregion
}
