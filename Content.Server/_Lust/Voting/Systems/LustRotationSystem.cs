using Content.Server.GameTicking;
using Content.Shared._Lust.LustCCVars;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;

namespace Content.Server._Lust.Voting.Systems;

public sealed class LustPresetPoolRotationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private GameTicker? _ticker;
    private GameRunLevel _lastRunLevel;
    private string? _lastStartedPresetId;

    public override void Initialize()
    {
        base.Initialize();

        _ticker = _entityManager.System<GameTicker>();
        _lastRunLevel = _ticker.RunLevel;

        var currentPool = _cfg.GetCVar(SunriseCCVars.GamePresetPool);

        if (currentPool != (string)_cfg.GetCVar(LustCCVars.LustAltVoteMainPreset) &&
            currentPool != (string)_cfg.GetCVar(LustCCVars.LustAltVotePeacefulPreset))
        {
            SetPool(_cfg.GetCVar(LustCCVars.LustAltVoteMainPreset));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_ticker == null)
            return;

        var current = _ticker.RunLevel;

        if (current == _lastRunLevel)
            return;

        if (_lastRunLevel == GameRunLevel.PreRoundLobby &&
            current != GameRunLevel.PreRoundLobby)
        {
            _lastStartedPresetId = _ticker.Preset?.ID;
        }

        if (_lastRunLevel != GameRunLevel.PreRoundLobby &&
            current == GameRunLevel.PreRoundLobby)
        {
            string nextPool;
            if (_cfg.GetCVar(LustCCVars.LustPeacefulAlternation))
            {
                nextPool = IsPeacefulPreset(_lastStartedPresetId)
                    ? _cfg.GetCVar(LustCCVars.LustAltVoteMainPreset)
                    : _cfg.GetCVar(LustCCVars.LustAltVotePeacefulPreset);
            }
            else
            {
                nextPool = _cfg.GetCVar(LustCCVars.LustAltVoteMainPreset);
            }

            SetPool(nextPool);

            _ticker.ClearExcludedPresets();
        }

        _lastRunLevel = current;
    }

    private void SetPool(string poolId)
    {
        if (_cfg.GetCVar(SunriseCCVars.GamePresetPool) == poolId)
            return;

        _cfg.SetCVar(SunriseCCVars.GamePresetPool, poolId);
    }

    private static bool IsPeacefulPreset(string? presetId)
    {
        if (string.IsNullOrEmpty(presetId))
            return false;

        return presetId.ToLowerInvariant() switch
        {
            "greenshift" or "peaceful" or "peacefulgamepreset" => true,
            _ => false,
        };
    }
}
