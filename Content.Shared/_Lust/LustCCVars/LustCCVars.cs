using Content.Shared.Clothing.Components;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Lust.LustCCVars;

[CVarDefs]
public sealed partial class LustCCVars : CVars
{

    #region Alternative VoteManager Alternation

    public static readonly CVarDef<bool> LustAltVotingEnabled =
        CVarDef.Create("lust.alt_voting.enabled", true);

    public static readonly CVarDef<bool> LustPeacefulAlternation =
        CVarDef.Create("lust.alt_vote.peaceful_alternation", true);

    public static readonly CVarDef<string> LustAltVoteMainPreset =
        CVarDef.Create("lust.alt_vote.main_preset",
            "LustPresetPoolAltDefault",
            CVar.SERVERONLY);

    public static readonly CVarDef<string> LustAltVotePeacefulPreset =
        CVarDef.Create("lust.alt_vote.peaceful_preset",
            "LustPresetPeacefulOnly",
            CVar.SERVERONLY);

    #endregion

}
