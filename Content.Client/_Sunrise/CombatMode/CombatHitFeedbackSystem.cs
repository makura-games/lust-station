using System.Collections.Generic;
using Content.Shared._Sunrise.CombatMode;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Client.Audio;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;

namespace Content.Client._Sunrise.CombatMode;

public sealed partial class CombatHitFeedbackSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IResourceManager _resources = default!;

    private IReadOnlyCollection<string> _soundIds = [];
    private EntityUid? _lastFeedbackSound;
    private CombatHitFeedbackType? _lastFeedbackType;

    public override void Initialize()
    {
        base.Initialize();

        _soundIds = CombatHitFeedbackSoundCatalog.GetSoundIds(_resources);
        if (_soundIds.Count > 0)
        {
            NormalizeSoundPreference(SunriseCCVars.CombatHitFeedbackHitSound, CombatHitFeedbackSoundCatalog.DefaultHitSound);
            NormalizeSoundPreference(SunriseCCVars.CombatHitFeedbackCriticalSound, CombatHitFeedbackSoundCatalog.DefaultCriticalSound);
            NormalizeSoundPreference(SunriseCCVars.CombatHitFeedbackKillSound, CombatHitFeedbackSoundCatalog.DefaultKillSound);
        }
        SubscribeNetworkEvent<CombatHitFeedbackEvent>(OnFeedback);
    }

    private void NormalizeSoundPreference(CVarDef<string> preference, string fallback)
    {
        var selected = _configuration.GetCVar(preference);
        var resolved = CombatHitFeedbackSoundCatalog.ResolveSoundId(selected, _soundIds, fallback);
        if (selected != resolved)
            _configuration.SetCVar(preference, resolved);
    }

    public void PlayPreview(CombatHitFeedbackType type, string? selectedSoundId)
    {
        Play(type, selectedSoundId, preview: true);
    }

    private void OnFeedback(CombatHitFeedbackEvent ev, EntitySessionEventArgs args)
    {
        Play(ev.Type, GetConfiguredSound(ev.Type));
    }

    private string GetConfiguredSound(CombatHitFeedbackType type)
    {
        return type switch
        {
            CombatHitFeedbackType.Hit => _configuration.GetCVar(SunriseCCVars.CombatHitFeedbackHitSound),
            CombatHitFeedbackType.Critical => _configuration.GetCVar(SunriseCCVars.CombatHitFeedbackCriticalSound),
            CombatHitFeedbackType.Kill => _configuration.GetCVar(SunriseCCVars.CombatHitFeedbackKillSound),
            _ => CombatHitFeedbackSoundCatalog.DefaultHitSound,
        };
    }

    private void Play(CombatHitFeedbackType type, string? soundId, bool preview = false)
    {
        var enabled = type switch
        {
            CombatHitFeedbackType.Hit => _configuration.GetCVar(SunriseCCVars.CombatHitFeedbackHitEnabled),
            CombatHitFeedbackType.Critical => _configuration.GetCVar(SunriseCCVars.CombatHitFeedbackCriticalEnabled),
            CombatHitFeedbackType.Kill => _configuration.GetCVar(SunriseCCVars.CombatHitFeedbackKillEnabled),
            _ => false,
        };

        if ((!preview && !enabled) || _soundIds.Count == 0)
            return;

        var fallback = type switch
        {
            CombatHitFeedbackType.Hit => CombatHitFeedbackSoundCatalog.DefaultHitSound,
            CombatHitFeedbackType.Critical => CombatHitFeedbackSoundCatalog.DefaultCriticalSound,
            CombatHitFeedbackType.Kill => CombatHitFeedbackSoundCatalog.DefaultKillSound,
            _ => CombatHitFeedbackSoundCatalog.DefaultHitSound,
        };
        var selected = CombatHitFeedbackSoundCatalog.ResolveSoundId(soundId, _soundIds, fallback);
        var volume = CombatHitFeedbackSoundCatalog.NormalizeVolume(_configuration.GetCVar(SunriseCCVars.CombatHitFeedbackVolume));
        var audioParams = AudioParams.Default.WithVolume(SharedAudioSystem.GainToVolume(volume));

        // Завершаем предыдущий хит, чтобы он не перекрывал сигнал крита или убийства.
        if (!preview)
        {
            if (_audio.IsPlaying(_lastFeedbackSound))
            {
                if (!CombatHitFeedbackRules.HasPriorityOverOrEqual(type, _lastFeedbackType))
                    return;

                _audio.Stop(_lastFeedbackSound);
            }

            _lastFeedbackSound = null;
            _lastFeedbackType = null;
        }

        var audio = _audio.PlayGlobal(
            new SoundPathSpecifier(CombatHitFeedbackSoundCatalog.GetSoundPath(selected)),
            Filter.Local(),
            false,
            audioParams);

        if (!preview)
        {
            _lastFeedbackSound = audio?.Entity;
            _lastFeedbackType = type;
        }
    }
}
