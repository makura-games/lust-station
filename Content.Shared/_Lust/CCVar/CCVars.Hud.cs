using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<int> HudHeldItemShowMode =
        CVarDef.Create("hud.held_item_show_mode", 0, CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<bool> HudShowGunHeatIndicator =
        CVarDef.Create("hud.show_gun_heat_indicator", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<bool> HudShowGunJamIndicator =
        CVarDef.Create("hud.show_gun_jam_indicator", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<bool> HudShowGunBoltIndicator =
        CVarDef.Create("hud.show_gun_bolt_indicator", true, CVar.ARCHIVE | CVar.CLIENTONLY);
}
