using Content.Client.CombatMode;
using Content.Client.Hands.Systems;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Configuration;

namespace Content.Client._Lust.Combat;

public sealed partial class GunCombatIndicatorsSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IEyeManager _eye = default!;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_cfg, CCVars.CombatModeIndicatorsPointShow, OnIndicatorsChanged);
        Subs.CVar(_cfg, CCVars.HudShowGunHeatIndicator, OnIndicatorsChanged);
        Subs.CVar(_cfg, CCVars.HudShowGunJamIndicator, OnIndicatorsChanged);
        Subs.CVar(_cfg, CCVars.HudShowGunBoltIndicator, OnIndicatorsChanged);
        UpdateOverlay();
    }

    public override void Shutdown()
    {
        _overlayManager.RemoveOverlay<GunCombatIndicatorsOverlay>();
        base.Shutdown();
    }

    private void OnIndicatorsChanged(bool _)
    {
        UpdateOverlay();
    }

    private void UpdateOverlay()
    {
        _overlayManager.RemoveOverlay<GunCombatIndicatorsOverlay>();
        if (!_cfg.GetCVar(CCVars.CombatModeIndicatorsPointShow))
            return;

        _overlayManager.AddOverlay(new GunCombatIndicatorsOverlay(
            _input,
            EntityManager,
            _eye,
            EntityManager.System<CombatModeSystem>(),
            EntityManager.System<HandsSystem>(),
            _cfg.GetCVar(CCVars.HudShowGunHeatIndicator),
            _cfg.GetCVar(CCVars.HudShowGunJamIndicator),
            _cfg.GetCVar(CCVars.HudShowGunBoltIndicator)));
    }
}
