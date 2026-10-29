using SunriseConfig = Content.Shared._Sunrise.SunriseCCVars.SunriseCCVars;
using Content.Shared.CombatMode;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._Sunrise.Interaction;

public sealed partial class ClickPlacementSystem : EntitySystem
{
    [Dependency] private INetConfigurationManager _configuration = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedPlayerManager _player = default!;

    /// <summary>
    /// Проверяет личную настройку размещения по клику; явное перекладывание её не использует.
    /// </summary>
    public bool ShouldBlock(EntityUid user)
    {
        if (TryComp<CombatModeComponent>(user, out var combat) && combat.IsInCombatMode)
            return false;

        if (_net.IsClient)
            return user == _player.LocalEntity && _configuration.GetCVar(SunriseConfig.BlockClickPlacement);

        return TryComp<ActorComponent>(user, out var actor) &&
               _configuration.GetClientCVar(actor.PlayerSession.Channel, SunriseConfig.BlockClickPlacement);
    }
}
