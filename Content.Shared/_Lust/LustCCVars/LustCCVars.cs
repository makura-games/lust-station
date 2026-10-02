using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Lust.LustCCVars;

[CVarDefs]
public sealed partial class LustCCVars : CVars
{
    /*
     * Game
     */
    public static readonly CVarDef<bool> LustGamePresetAlternationEnabled =
        CVarDef.Create("lust.game.preset_alternation_enabled", false);

    public static readonly CVarDef<string> LustGreenshiftPreset =
        CVarDef.Create("lust.game.greenshift_preset", "Greenshift", CVar.SERVERONLY);

    public static readonly CVarDef<string> LustGamePresetPool =
        CVarDef.Create("lust.game.preset_pool", "LustDefaultPresetPool", CVar.SERVERONLY);

}
