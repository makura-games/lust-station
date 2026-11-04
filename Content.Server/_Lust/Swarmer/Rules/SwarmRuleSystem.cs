using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared._Lust.Swarmer.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;

namespace Content.Server._Lust.Swarmer.Rules;

/// <summary>
/// Adds the size of the Swarm to the round end summary.
/// </summary>
public sealed partial class SwarmRuleSystem : GameRuleSystem<SwarmRuleComponent>
{
    [Dependency] private MobStateSystem _mobState = default!;

    protected override void AppendRoundEndText(EntityUid uid, SwarmRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        args.AddLine(Loc.GetString("swarm-round-end-alive", ("count", CountAliveSwarmers())));
    }

    /// <summary>
    /// Counts swarmers that are alive and controlled by a player. Dormant shells waiting for a ghost don't count.
    /// </summary>
    public int CountAliveSwarmers()
    {
        var count = 0;
        var query = EntityQueryEnumerator<SwarmerComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState))
        {
            if (_mobState.IsAlive(uid, mobState) && HasComp<ActorComponent>(uid))
                count++;
        }

        return count;
    }
}
