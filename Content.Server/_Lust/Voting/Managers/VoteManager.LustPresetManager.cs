using Content.Server._Sunrise.Presets;
using Content.Server._Sunrise.Storyteller.Systems;
using Content.Server.GameTicking;
using Content.Shared._Lust.LustCCVars;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Player;

namespace Content.Server.Voting.Managers;

public sealed partial class VoteManager
{
    private Dictionary<string, string> GetLustRegularPresetsForVote(
        IReadOnlySet<string>? excludedPresets = null)
    {
        var ticker = _entityManager.System<GameTicker>();

        ticker.ForceGreenshiftPresetVote = ticker.Preset?.ID != "Greenshift";

        var presetPoolId = _cfg.GetCVar(LustCCVars.LustGamePresetPool);

        if (!_prototypeManager.TryIndex<GamePresetPoolPrototype>(presetPoolId, out var presetPoolProto))
            return new Dictionary<string, string>();

        var eligiblePresets = ticker.GetEligibleVotePresets(
            presetPoolProto.Presets,
            _playerManager.PlayerCount,
            excludedPresets);

        var result = new Dictionary<string, string>();

        foreach (var (presetId, title) in eligiblePresets)
        {
            if (!StorytellerSystem.IsStorytellerPreset(presetId))
                result[presetId] = title;
        }

        return result;
    }
}
