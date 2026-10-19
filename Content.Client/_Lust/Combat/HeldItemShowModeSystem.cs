using Content.Shared.CCVar;
using Content.Shared._Lust.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client._Lust.Combat;

public sealed partial class HeldItemShowModeSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();
        if (_cfg.GetCVar(CCVars.HudHeldItemShow))
            return;

        if (_cfg.GetCVar(CCVars.HudHeldItemShowMode) == (int) HeldItemShowMode.Always)
            _cfg.SetCVar(CCVars.HudHeldItemShowMode, (int) HeldItemShowMode.Never);

        _cfg.SetCVar(CCVars.HudHeldItemShow, true);
        _cfg.SaveToFile();
    }
}
