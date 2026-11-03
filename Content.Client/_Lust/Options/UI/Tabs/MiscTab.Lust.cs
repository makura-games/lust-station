using Content.Shared._Lust.CCVar;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared;

#pragma warning disable IDE0130 // Namespace не совпадает с расположением partial-файла
namespace Content.Client.Options.UI.Tabs;

public sealed partial class MiscTab
{
    private void InitializeLustOptions()
    {
        Control.AddOptionDropDown(
            CCVars.HudHeldItemShowMode,
            HeldItemShowModeDropDown,
            [
                new OptionDropDownCVar<int>.ValueOption((int) HeldItemShowMode.Always, Loc.GetString("ui-options-held-item-show-mode-always")),
                new OptionDropDownCVar<int>.ValueOption((int) HeldItemShowMode.CombatModeOnly, Loc.GetString("ui-options-held-item-show-mode-combat-only")),
                new OptionDropDownCVar<int>.ValueOption((int) HeldItemShowMode.NonCombatModeOnly, Loc.GetString("ui-options-held-item-show-mode-non-combat-only")),
                new OptionDropDownCVar<int>.ValueOption((int) HeldItemShowMode.Never, Loc.GetString("ui-options-held-item-show-mode-never")),
            ]);
        Control.AddOptionCheckBox(CCVars.CombatModeIndicatorsPointShow, ShowCombatModeIndicatorsCheckBox);
        Control.AddOptionCheckBox(CCVars.HudShowGunHeatIndicator, ShowGunHeatIndicatorCheckBox);
        Control.AddOptionCheckBox(CCVars.HudShowGunJamIndicator, ShowGunJamIndicatorCheckBox);
        Control.AddOptionCheckBox(CCVars.HudShowGunBoltIndicator, ShowGunBoltIndicatorCheckBox);
    }
}
