using System.Text;
using Content.Server._Sunrise.Anomaly.Components;
using Content.Server._Sunrise.TTS;
using Content.Server.Anomaly;
using Content.Server.Atmos.Components;
using Content.Server.Chat.Systems;
using Content.Server.Flash;
using Content.Server.Light.EntitySystems;
using Content.Server.Radio;
using Content.Server.Speech.EntitySystems;
using Content.Shared._Sunrise.Anomaly.Components;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Drunk;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Jittering;
using Content.Shared.Light.Components;
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
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Anomaly.Systems;

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
    [Dependency] private PoweredLightSystem _poweredLight = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private TTSSystem _tts = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private AnomalySystem _anomaly = default!;

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
        new SoundPathSpecifier("/Audio/Effects/glass_step.ogg"),
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoundAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<SoundAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<SoundAnomalyComponent, AnomalyShutdownEvent>(OnShutdown);
        SubscribeLocalEvent<SoundAnomalyComponent, ListenEvent>(OnListen);

        SubscribeLocalEvent<SoundAnomalyCoreComponent, UseInHandEvent>(OnCoreUseInHand);
        SubscribeLocalEvent<SoundAnomalyResonanceComponent, MoveEvent>(OnResonanceMove);

        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);

        SubscribeLocalEvent<AcousticProtectionComponent, ExaminedEvent>(OnAcousticProtectionExamined);
    }

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

                var meta = MetaData(item.Value);
                if (meta.EntityPrototype != null)
                {
                    var protoId = meta.EntityPrototype.ID;
                    if (protoId.Contains("Headphones", StringComparison.OrdinalIgnoreCase) ||
                        protoId.Contains("HeadsetAlt", StringComparison.OrdinalIgnoreCase) ||
                        protoId.Contains("Hardhat", StringComparison.OrdinalIgnoreCase) ||
                        protoId.Contains("Hardsuit", StringComparison.OrdinalIgnoreCase) ||
                        protoId.Contains("EVA", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                if (_tag.HasTag(item.Value, HelmetEvaTag) || _tag.HasTag(item.Value, SecurityHelmetTag))
                    return true;
            }
        }

        return false;
    }

    private void OnAcousticProtectionExamined(Entity<AcousticProtectionComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("acoustic-protection-examine"));
    }

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

    private void OnListen(Entity<SoundAnomalyComponent> ent, ref ListenEvent args)
    {
        // Игнорируем сообщения от самой аномалии или пустые реплики
        if (args.Source == ent.Owner || string.IsNullOrWhiteSpace(args.Message))
            return;

        var comp = ent.Comp;
        var curTime = _timing.CurTime;

        // Акустическое возбуждение от услышанной речи
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

    private void OnShutdown(Entity<SoundAnomalyComponent> ent, ref AnomalyShutdownEvent args)
    {
        if (ent.Comp.HeardRecords.Count > 0)
        {
            _latestAnomalyRecords = new(ent.Comp.HeardRecords);
        }
    }

    private void OnCoreUseInHand(Entity<SoundAnomalyCoreComponent> coreEnt, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryUseCore(coreEnt, args.User))
            args.Handled = true;
    }

    /// <summary>
    /// Публичный метод попытки воспроизведения звукового эха из ядра.
    /// </summary>
    public bool TryUseCore(Entity<SoundAnomalyCoreComponent> coreEnt, EntityUid user)
    {
        if (!CanUseCore(coreEnt, user))
            return false;

        var core = coreEnt.Comp;
        var curTime = _timing.CurTime;
        core.NextUseTime = curTime + core.Cooldown;

        // Если ядро еще не имеет сохраненных записей, берем последние записи от аномалии
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
            // Погасшее инертное ядро: воспроизводит тихо с радиопомехами
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
            // Активное ядро: воспроизводит звонко и четко голосом автора
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

        return true;
    }

    /// <summary>
    /// Проверка возможности использования ядра звуковой аномалии.
    /// </summary>
    public bool CanUseCore(Entity<SoundAnomalyCoreComponent> coreEnt, EntityUid user, bool quiet = false)
    {
        if (_timing.CurTime < coreEnt.Comp.NextUseTime)
            return false;

        return true;
    }

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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

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

            var entities = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic | LookupFlags.Sundries);
            var hasAudience = false;
            foreach (var ent in entities)
            {
                if (HasComp<ActorComponent>(ent) || HasComp<MobStateComponent>(ent))
                {
                    hasAudience = true;
                    break;
                }
            }

            // Проверка бега персонажей в пределах RunningDetectionRadius (по умолчанию 4 клетки).
            // Персонажи, передвигающиеся шагом (с зажатым Shift), крадутся бесшумно и не будят аномалию!
            var nearbyEntities = _lookup.GetEntitiesInRange(uid, sound.RunningDetectionRadius, LookupFlags.Dynamic);
            var isSomeoneRunning = false;
            foreach (var ent in nearbyEntities)
            {
                if (!HasComp<MobStateComponent>(ent))
                    continue;

                if (TryComp<InputMoverComponent>(ent, out var mover) &&
                    mover.HasDirectionalMovement &&
                    mover.Sprinting)
                {
                    isSomeoneRunning = true;
                    break;
                }
            }

            if (isSomeoneRunning)
            {
                sound.LastSoundActivityTime = curTime;
                if (sound.IsDormant)
                {
                    sound.IsDormant = false;
                    _popup.PopupEntity(Loc.GetString("sound-anomaly-agitated-running"), uid, PopupType.MediumCaution);
                }

                // Умеренное повышение нестабильности при беге рядом (раз в 1.5 секунды)
                if (curTime - sound.LastRunningAgitationTime >= TimeSpan.FromSeconds(1.5))
                {
                    sound.LastRunningAgitationTime = curTime;
                    _anomaly.ChangeAnomalyStability(uid, 0.03f, anom);
                }
            }

            // Режим тишины: если вокруг тихо дольше SilenceThreshold, аномалия успокаивается и засыпает
            var isSilent = (curTime - sound.LastSoundActivityTime) > sound.SilenceThreshold;
            if (isSilent)
            {
                if (!sound.IsDormant)
                {
                    sound.IsDormant = true;
                    _popup.PopupEntity(Loc.GetString("sound-anomaly-dormant"), uid, PopupType.Small);
                }

                // В полной тишине нестабильность медленно снижается
                if (anom.Stability > 0.05f)
                {
                    var stabilityDrop = sound.SilenceStabilityDecayRate * frameTime;
                    _anomaly.ChangeAnomalyStability(uid, -stabilityDrop, anom);
                }
            }
            else
            {
                sound.IsDormant = false;
            }

            // Масштабирование частоты спама звуков по процентам (Severity / Stability)
            // Ближе к 100%: бешено спамит звуками (интервал ~0.28 сек)
            // На низких процентах (~0%): раз в ~3 секунды
            var intensity = Math.Clamp(MathF.Max(anom.Severity, anom.Stability), 0f, 1f);
            var currentIntervalSeconds = MathHelper.Lerp(3.0f, 0.28f, intensity);

            // Если аномалия спит в тишине - интервал увеличивается
            if (sound.IsDormant)
            {
                currentIntervalSeconds *= 1.8f;
            }

            // 1. Пассивные пространственные звуки-фантомы
            if (curTime >= sound.NextPassiveSoundTime)
            {
                sound.NextPassiveSoundTime = curTime + TimeSpan.FromSeconds(currentIntervalSeconds);

                if (hasAudience && sound.PhantomSounds.Count > 0 && _random.Prob(sound.PassivePhantomSoundChance))
                {
                    var offset = new System.Numerics.Vector2(_random.NextFloat(-1.5f, 1.5f), _random.NextFloat(-1.5f, 1.5f));
                    var soundCoords = xform.Coordinates.Offset(offset);
                    var pickedSound = _random.Pick(sound.PhantomSounds);

                    // Если она спит - звуки издаются очень тихо
                    float volume;
                    float maxDist;
                    if (sound.IsDormant)
                    {
                        volume = -6f; // Очень тихо во сне
                        maxDist = 3.5f; // Слышно только вплотную
                    }
                    else
                    {
                        volume = MathHelper.Lerp(2.0f, 6.0f, intensity);
                        maxDist = range;
                    }

                    var audioParams = AudioParams.Default
                        .WithVolume(volume)
                        .WithMaxDistance(maxDist)
                        .WithVariation(0.08f);

                    _audio.PlayPvs(pickedSound, soundCoords, audioParams);
                }
            }

            // 2. Эхолалия: искажение и выкрикивание услышанных фраз с копированием TTS голоса (во сне не кричит)
            if (!sound.IsDormant && curTime >= sound.NextEchoTime)
            {
                var echoIntervalSeconds = MathHelper.Lerp(6.0f, 2.0f, intensity);
                sound.NextEchoTime = curTime + TimeSpan.FromSeconds(echoIntervalSeconds) + TimeSpan.FromSeconds(_random.NextFloat(-0.5f, 1.0f));

                if (hasAudience && _random.Prob(sound.EchoChance))
                {
                    string chosenMessage;
                    string? chosenVoice;

                    if (sound.HeardRecords.Count > 0)
                    {
                        var record = _random.Pick(sound.HeardRecords);
                        chosenMessage = record.Message;
                        chosenVoice = record.VoiceId ?? sound.DefaultVoice;
                    }
                    else if (sound.DefaultEchoPhrases.Count > 0)
                    {
                        chosenMessage = _random.Pick(sound.DefaultEchoPhrases);
                        chosenVoice = sound.DefaultVoice;
                    }
                    else
                    {
                        chosenMessage = string.Empty;
                        chosenVoice = sound.DefaultVoice;
                    }

                    if (!string.IsNullOrWhiteSpace(chosenMessage))
                    {
                        var distorted = DistortMessage(chosenMessage);

                        var tts = EnsureComp<TTSComponent>(uid);
                        tts.VoicePrototypeId = chosenVoice;

                        _audio.PlayPvs(sound.EchoSound, uid, AudioParams.Default.WithVolume(3.5f).WithVariation(0.1f));
                        _chat.TrySendInGameICMessage(
                            uid,
                            distorted,
                            InGameICChatType.Speak,
                            ChatTransmitRange.Normal,
                            nameOverride: Loc.GetString("sound-anomaly-echo-speaker-name"),
                            ignoreActionBlocker: true);
                    }
                }
            }
        }

        // Поддержание компонента резонанса шагов у персонажей рядом с аномалией
        foreach (var (coords, radius) in activeAnomalyPositions)
        {
            var nearbyMobs = _lookup.GetEntitiesInRange(coords, radius, LookupFlags.Dynamic);
            foreach (var mob in nearbyMobs)
            {
                if (HasComp<MobStateComponent>(mob))
                {
                    EnsureComp<SoundAnomalyResonanceComponent>(mob);
                }
            }
        }

        // 3. Обработка слуховых галлюцинаций в голове у контуженных игроков и кукол
        var hauntedQuery = EntityQueryEnumerator<SoundAnomalyHauntedComponent, TransformComponent>();
        while (hauntedQuery.MoveNext(out var victim, out var haunted, out var xform))
        {
            // Возврат исходного голоса жертве по истечении 5 минут
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

            // 50% фантомный звук, 50% шепот фраз
            if (_random.Prob(0.5f) && sourceComp != null && sourceComp.PhantomSounds.Count > 0)
            {
                var sound = _random.Pick(sourceComp.PhantomSounds);
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
            else if (sourceComp != null)
            {
                string phrase;
                string voice;
                if (sourceComp.HeardRecords.Count > 0)
                {
                    var rec = _random.Pick(sourceComp.HeardRecords);
                    phrase = rec.Message;
                    voice = rec.VoiceId ?? sourceComp.DefaultVoice;
                }
                else if (sourceComp.DefaultEchoPhrases.Count > 0)
                {
                    phrase = _random.Pick(sourceComp.DefaultEchoPhrases);
                    voice = sourceComp.DefaultVoice;
                }
                else
                {
                    phrase = string.Empty;
                    voice = sourceComp.DefaultVoice;
                }

                if (!string.IsNullOrWhiteSpace(phrase))
                {
                    var whisperPhrase = "... " + phrase.ToLowerInvariant() + " ...";

                    _chat.TrySendInGameICMessage(
                        victim,
                        whisperPhrase,
                        InGameICChatType.Whisper,
                        ChatTransmitRange.Normal,
                        nameOverride: null,
                        checkRadioPrefix: false,
                        ignoreActionBlocker: true);

                    if (TryComp<ActorComponent>(victim, out var actor) &&
                        _prototypeManager.TryIndex<TTSVoicePrototype>(voice, out var voiceProto))
                    {
                        PlayPersonalHallucinationTts(actor.PlayerSession, phrase, voiceProto, 0.35f);
                        _popup.PopupEntity(Loc.GetString("sound-anomaly-hallucination-whisper"), victim, victim, PopupType.SmallCaution);
                    }
                }
            }
        }
    }

    private async void PlayPersonalHallucinationTts(ICommonSession session, string phrase, TTSVoicePrototype voiceProto, float volume = 0.35f)
    {
        try
        {
            var whisperPhrase = "... " + phrase.ToLowerInvariant() + " ...";
            var soundData = await _tts.GenerateTTS(whisperPhrase, voiceProto);
            if (soundData != null)
            {
                RaiseNetworkEvent(new PlayTTSEvent(soundData, null, false, volume), session);
            }
        }
        catch
        {
            // Игнорируем сбои генерации TTS в фоновых эффектах
        }
    }

    private void OnPulse(Entity<SoundAnomalyComponent> ent, ref AnomalyPulseEvent args)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;
        var range = comp.PulseRadius * MathF.Max(0.5f, args.Severity) * args.PowerModifier;
        var worldPos = _xform.GetWorldPosition(uid);

        // Звук пульсации
        _audio.PlayPvs(comp.PulseSound, uid);

        var entities = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Static);
        foreach (var target in entities)
        {
            if (target == uid)
                continue;

            // Резонансный взрыв лампочек в светильниках
            if (TryComp<PoweredLightComponent>(target, out var light))
            {
                if (_random.Prob(comp.LightBreakChance))
                {
                    _poweredLight.TryDestroyBulb(target, light);
                }
            }

            // Перегрузка и искрение радиоустройств, интеркомов и гарнитур на 30 секунд
            if (IsRadioDevice(target))
            {
                var overloaded = EnsureComp<SoundAnomalyOverloadedRadioComponent>(target);
                overloaded.OverloadedUntil = _timing.CurTime + TimeSpan.FromSeconds(30);
                Spawn("EffectSparks", _xform.GetMapCoordinates(target));
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/sparks1.ogg"), target, AudioParams.Default.WithVolume(2f));
            }

            // Разрушение окон, стеклянных перегородок и посуды
            if (IsGlassObject(target))
            {
                if (comp.PulseWindowDamage.GetTotal() > 0)
                {
                    _damageable.TryChangeDamage(target, comp.PulseWindowDamage, ignoreResistances: true, origin: uid);
                }
            }

            // Дрожь и сбрасывание незакрепленных предметов со столов и полок
            if (HasComp<ItemComponent>(target) && !Transform(target).Anchored && !_container.IsEntityInContainer(target))
            {
                _jitter.DoJitter(target, TimeSpan.FromSeconds(2.5), true);
                var pushDir = _xform.GetWorldPosition(target) - worldPos;
                if (pushDir.LengthSquared() > 0.01f)
                {
                    _throwing.TryThrow(target, pushDir.Normalized() * _random.NextFloat(1.5f, 3.5f), 2.5f, uid, 0);
                }
            }

            // Полный спектр акустической контузии для живых существ и кукол
            if (HasComp<MobStateComponent>(target) || HasComp<ActorComponent>(target))
            {
                var severity = MathF.Max(0.1f, args.Severity);

                // Защита слуха (наушники, каски, гермошлемы)
                if (HasAcousticProtection(target))
                {
                    _stamina.TakeStaminaDamage(target, 15f);
                    _popup.PopupEntity(Loc.GetString("sound-anomaly-protected-ears"), target, target, PopupType.Medium);
                    continue; // Защищены от смены голоса TTS, галлюцинаций и контузии!
                }

                var jitterTime = TimeSpan.FromSeconds(5 + 5 * severity);
                var drunkTime = TimeSpan.FromSeconds(8 + 8 * severity);
                var stutterTime = TimeSpan.FromSeconds(10 + 10 * severity);
                var flashTime = TimeSpan.FromMilliseconds(2000 + 2000 * severity);
                var stunTime = TimeSpan.FromSeconds(3 + 2 * severity);
                var knockdownTime = TimeSpan.FromSeconds(2.5 + 2 * severity);

                // Оглушение, сбивание с ног (кукла падает, роняет вещи, видит звезды)
                _stun.TryKnockdown(target, knockdownTime, force: true);
                _stun.TryAddStunDuration(target, stunTime);
                _stamina.TakeStaminaDamage(target, 45f + 30f * severity);

                // Тряска и сильное головокружение
                _jitter.DoJitter(target, jitterTime, true);
                _drunk.TryApplyDrunkenness(target, drunkTime);
                _stutter.DoStutter(target, stutterTime, true);

                // Ослепляюще-глушащая акустическая вспышка
                _flash.Flash(target, null, null, flashTime, 0.5f);

                // Слуховые галлюцинации в голове у жертвы или куклы на 40-60 секунд
                var haunted = EnsureComp<SoundAnomalyHauntedComponent>(target);
                haunted.SourceAnomaly = uid;
                haunted.EndTime = _timing.CurTime + TimeSpan.FromSeconds(40 + 20 * severity);
                haunted.NextHallucinationTime = _timing.CurTime + TimeSpan.FromSeconds(1f);

                // Смена TTS голоса на 5 минут на последний запомненный голос (кроме SCP049)
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

                // Непроизвольный крик от болевого шока в ушах
                if (_random.Prob(comp.ScreamChance))
                {
                    _chat.TryEmoteWithChat(target, "Scream", ignoreActionBlocker: true);
                }

                // Громкий оглушающий звон в ушах (слышен всем вокруг в PVS)
                _audio.PlayPvs(comp.EarRingingSound, target, AudioParams.Default.WithVolume(10f));
                _popup.PopupEntity(Loc.GetString("sound-anomaly-ringing-ears"), target, PopupType.LargeCaution);
            }
        }
    }

    private void OnSupercritical(Entity<SoundAnomalyComponent> ent, ref AnomalySupercriticalEvent args)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;
        var range = comp.SupercriticalRadius * args.PowerModifier;
        var strength = comp.SupercriticalPushStrength * args.PowerModifier;
        var worldPos = _xform.GetWorldPosition(uid);

        // Отключение связи на всей станции на 5 минут (эффект солнечной вспышки)
        _globalRadioBlackoutUntil = _timing.CurTime + TimeSpan.FromMinutes(5);

        // Мощный звук взрыва сверхзвуковой волны
        _audio.PlayPvs(comp.SupercriticalSound, uid);

        var entities = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic | LookupFlags.Static);
        foreach (var target in entities)
        {
            if (target == uid)
                continue;

            // Уничтожение всех лампочек в радиусе суперкритики
            if (TryComp<PoweredLightComponent>(target, out var light))
            {
                _poweredLight.TryDestroyBulb(target, light);
            }

            // Выбивание и разрушение окон/стекол
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

            // Оглушение, сбивание с ног, вспышка контузии и звон
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

                    // Постоянные слуховые галлюцинации в радиусе 10 клеток навсегда до конца раунда
                    var distToTarget = (_xform.GetWorldPosition(target) - worldPos).Length();
                    if (distToTarget <= 10f)
                    {
                        var permHaunted = EnsureComp<SoundAnomalyHauntedComponent>(target);
                        permHaunted.SourceAnomaly = uid;
                        permHaunted.EndTime = TimeSpan.MaxValue; // Навсегда до конца раунда!
                        permHaunted.NextHallucinationTime = _timing.CurTime + TimeSpan.FromSeconds(1f);
                        _popup.PopupEntity(Loc.GetString("sound-anomaly-permanent-whisper"), target, target, PopupType.LargeCaution);
                    }
                    else
                    {
                        var haunted = EnsureComp<SoundAnomalyHauntedComponent>(target);
                        haunted.SourceAnomaly = uid;
                        haunted.EndTime = _timing.CurTime + TimeSpan.FromSeconds(90);
                        haunted.NextHallucinationTime = _timing.CurTime + TimeSpan.FromSeconds(1f);
                    }

                    _audio.PlayPvs(comp.EarRingingSound, target, AudioParams.Default.WithVolume(15f));
                    _popup.PopupEntity(Loc.GetString("sound-anomaly-eardrum-rupture"), target, PopupType.LargeCaution);
                }
            }
        }
    }

    private bool IsRadioDevice(EntityUid uid)
    {
        return HasComp<HeadsetComponent>(uid) ||
               HasComp<IntercomComponent>(uid) ||
               HasComp<RadioMicrophoneComponent>(uid) ||
               HasComp<RadioSpeakerComponent>(uid);
    }

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

    private string DistortMessage(string original)
    {
        if (string.IsNullOrWhiteSpace(original))
            return "...";

        var words = original.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            if (word.Length == 0)
                continue;

            // Эхо-заикание в начале слова (35% шанс)
            if (_random.Prob(0.35f) && word.Length > 2)
            {
                var prefix = word.Substring(0, Math.Min(2, word.Length));
                sb.Append(prefix).Append("-").Append(prefix).Append("...");
            }

            // Растягивание гласных букв
            foreach (var c in word)
            {
                sb.Append(c);
                if ("аеёиоуыэюяaeiouyАЕЁИОУЫЭЮЯAEIOUY".Contains(c) && _random.Prob(0.35f))
                {
                    sb.Append(c);
                    if (_random.Prob(0.4f))
                        sb.Append(c);
                }
            }

            // Шанс отголоска-многоточия
            if (_random.Prob(0.25f))
                sb.Append("...");

            if (i < words.Length - 1)
                sb.Append(' ');
        }

        return sb.ToString();
    }
}
