using Content.Client.CombatMode;
using Content.Shared._Lust.CCVar;
using Content.Shared.CCVar;

namespace Content.Client.Hands;

public sealed partial class ShowHandItemOverlay
{
    private CombatModeSystem? _combatMode;

    private bool ShouldShowHeldItem()
    {
        var mode = (HeldItemShowMode) _cfg.GetCVar(CCVars.HudHeldItemShowMode);
        return mode switch
        {
            HeldItemShowMode.Always => true,
            HeldItemShowMode.CombatModeOnly => IsInCombatMode(),
            HeldItemShowMode.NonCombatModeOnly => !IsInCombatMode(),
            _ => false,
        };
    }

    private bool IsInCombatMode()
    {
        _combatMode ??= _entMan.System<CombatModeSystem>();
        return _combatMode.IsInCombatMode();
    }
}
