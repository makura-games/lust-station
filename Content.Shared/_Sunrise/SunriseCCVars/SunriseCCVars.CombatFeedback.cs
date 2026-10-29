using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    public static readonly CVarDef<bool> CombatHitFeedbackHitEnabled =
        CVarDef.Create("sunrise.combat_feedback.hit_enabled", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CombatHitFeedbackCriticalEnabled =
        CVarDef.Create("sunrise.combat_feedback.critical_enabled", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CombatHitFeedbackKillEnabled =
        CVarDef.Create("sunrise.combat_feedback.kill_enabled", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> CombatHitFeedbackHitSound =
        CVarDef.Create("sunrise.combat_feedback.hit_sound", "hit", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> CombatHitFeedbackCriticalSound =
        CVarDef.Create("sunrise.combat_feedback.critical_sound", "critical", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> CombatHitFeedbackKillSound =
        CVarDef.Create("sunrise.combat_feedback.kill_sound", "kill", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> CombatHitFeedbackVolume =
        CVarDef.Create("sunrise.combat_feedback.volume", 0.75f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CombatModeBlockPickup =
        CVarDef.Create("sunrise.combat_mode.block_pickup", false, CVar.CLIENT | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CombatModeBlockDrop =
        CVarDef.Create("sunrise.combat_mode.block_drop", false, CVar.CLIENT | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<bool> BlockClickPlacement =
        CVarDef.Create("sunrise.interaction.block_click_placement", false, CVar.CLIENT | CVar.REPLICATED | CVar.ARCHIVE);
}
