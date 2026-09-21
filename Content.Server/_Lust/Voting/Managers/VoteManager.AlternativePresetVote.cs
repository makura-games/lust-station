using System.Linq;
using Content.Server._Sunrise.Presets;
using Content.Server.GameTicking;
using Content.Shared._Lust.LustCCVars;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Database;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Voting.Managers;

public sealed partial class VoteManager
{
    private bool IsLustPresetPoolActive()
    {
        var poolId = _cfg.GetCVar(SunriseCCVars.GamePresetPool);

        return _cfg.GetCVar(LustCCVars.LustAltVotingEnabled) &&
            (poolId == _cfg.GetCVar(LustCCVars.LustAltVoteMainPreset) ||
            poolId == _cfg.GetCVar(LustCCVars.LustAltVotePeacefulPreset));
    }

    private Dictionary<string, string> GetLustEligiblePresets()
    {
        var ticker = _entityManager.System<GameTicker>();
        var poolId = _cfg.GetCVar(SunriseCCVars.GamePresetPool);

        if (!_prototypeManager.TryIndex<GamePresetPoolPrototype>(poolId, out var pool))
            return new Dictionary<string, string>();

        var poolPresets = new Dictionary<string, MinMax>(pool.Presets);

        return ticker.GetEligibleVotePresets(
            poolPresets,
            _playerManager.PlayerCount,
            null);
    }

    private bool CanCallLustPresetVote()
    {
        var presets = GetLustEligiblePresets();

        switch (presets.Count)
        {
            case 0:
                return false;
            case > 1:
                return true;
        }

        var ticker = _entityManager.System<GameTicker>();
        var singlePresetId = presets.Keys.First();

        return singlePresetId != ticker.Preset?.ID;
    }

    private static string GetLustPresetDisplayNameLocId(string presetId, string fallbackTitleLocId)
    {
        return presetId.ToLowerInvariant() switch
        {
            "dynamic" => "lust-preset-dynamic",
            "secret" => "lust-preset-secret",
            "greenshift" => "lust-preset-peaceful",
            "storytellerclassic" => "ui-vote-storyteller-type-classic-name",
            "storytellerinsane" => "ui-vote-storyteller-type-insane-name",
            _ => fallbackTitleLocId,
        };
    }

    private bool TryCreateLustPresetVote(ICommonSession? initiator)
    {
        if (!IsLustPresetPoolActive())
            return false;

        var presets = GetLustEligiblePresets();

        switch (presets.Count)
        {
            case 0:
                Logger.Warning("Lust preset pool has no eligible game modes.");
                return true;
            case 1:
            {
                var singlePreset = presets.First();
                var displayName = Loc.GetString(
                    GetLustPresetDisplayNameLocId(singlePreset.Key, singlePreset.Value));

                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("ui-vote-gamemode-auto-set", ("preset", displayName)));

                _adminLogger.Add(
                    LogType.Vote,
                    LogImpact.Medium,
                    $"Lust preset vote skipped, auto-selected: {singlePreset.Key}");

                _entityManager.System<GameTicker>().SetGamePreset(singlePreset.Key);
                return true;
            }
        }

        var options = CreateSunrisePresetVoteOptions(
            Loc.GetString("ui-vote-gamemode-title"),
            initiator);

        foreach (var (presetId, titleLocId) in presets)
        {
            var displayName = Loc.GetString(
                GetLustPresetDisplayNameLocId(presetId, titleLocId));

            options.Options.Add((displayName, presetId));
        }

        var vote = CreateVote(options);

        vote.OnFinished += (_, args) =>
        {
            string picked;

            if (args.Winner == null)
            {
                picked = (string) _random.Pick(args.Winners);
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("ui-vote-gamemode-tie"));
            }
            else
            {
                picked = (string) args.Winner;
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("ui-vote-gamemode-win"));
            }

            _adminLogger.Add(
                LogType.Vote,
                LogImpact.Medium,
                $"Lust preset vote finished: {picked}");

            _entityManager.System<GameTicker>().SetGamePreset(picked);
        };

        return true;
    }
}
