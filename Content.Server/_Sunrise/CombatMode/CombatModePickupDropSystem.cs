using Content.Shared._Sunrise.CombatMode;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CombatMode;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.CombatMode;

public sealed partial class CombatModePickupDropSystem : SharedCombatModePickupDropSystem
{
    [Dependency] private INetConfigurationManager _configuration = default!;

    protected override bool ShouldBlockPickup(EntityUid uid, CombatModeComponent component)
    {
        return component.IsInCombatMode &&
               TryComp<ActorComponent>(uid, out var actor) &&
               _configuration.GetClientCVar(actor.PlayerSession.Channel, SunriseCCVars.CombatModeBlockPickup);
    }

    protected override bool ShouldBlockDrop(EntityUid uid, CombatModeComponent component)
    {
        return component.IsInCombatMode &&
               TryComp<ActorComponent>(uid, out var actor) &&
               _configuration.GetClientCVar(actor.PlayerSession.Channel, SunriseCCVars.CombatModeBlockDrop);
    }
}
