using Content.Shared._Sunrise.CombatMode;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CombatMode;
using Robust.Client.Player;
using Robust.Shared.Configuration;

namespace Content.Client._Sunrise.CombatMode;

public sealed partial class CombatModePickupDropSystem : SharedCombatModePickupDropSystem
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    protected override bool ShouldBlockPickup(EntityUid uid, CombatModeComponent component)
    {
        return CombatModePickupDropRules.ShouldBlock(
            _configuration.GetCVar(SunriseCCVars.CombatModeBlockPickup),
            component.IsInCombatMode,
            uid == _playerManager.LocalEntity);
    }

    protected override bool ShouldBlockDrop(EntityUid uid, CombatModeComponent component)
    {
        return CombatModePickupDropRules.ShouldBlock(
            _configuration.GetCVar(SunriseCCVars.CombatModeBlockDrop),
            component.IsInCombatMode,
            uid == _playerManager.LocalEntity);
    }
}
