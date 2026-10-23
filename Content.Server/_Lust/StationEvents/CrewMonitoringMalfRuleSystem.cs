using Content.Server._Lust.Medical;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class CrewMonitoringMalfRuleSystem
    : StationEventSystem<CrewMonitoringMalfRuleComponent>
{
    protected override void Started(
        EntityUid uid,
        CrewMonitoringMalfRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
            return;

        if (!TryComp<StationEventComponent>(uid, out var stationEvent) ||
            stationEvent.EndTime == null)
        {
            return;
        }

        if (component.MinPlayerFraction < 0 ||
            component.MaxPlayerFraction > 1 ||
            component.MinPlayerFraction > component.MaxPlayerFraction ||
            component.CriticalChance < 0 ||
            component.DeadChance < 0 ||
            component.PoorChance < 0 ||
            component.CriticalChance +
            component.DeadChance +
            component.PoorChance > 100)
        {
            Log.Error("Crew monitoring malfunction: invalid event settings.");
            return;
        }

        var candidates = new List<EntityUid>();

        // Запрос перебирает только сущности, имеющие одновременно:
        // ActorComponent     — управление подключённым игроком;
        // MobStateComponent  — состояние персонажа;
        // TransformComponent — положение и принадлежность к миру.

        var query =
            EntityQueryEnumerator<ActorComponent, MobStateComponent, TransformComponent>();

        while (query.MoveNext(
                   out var playerUid,
                   out _,
                   out var mobState,
                   out var xform))
        {   
            // Мёртвые персонажи не участвуют.
            // Критическое состояние не исключается

            if (mobState.CurrentState == MobState.Dead)
                continue;

            if (StationSystem.GetOwningStation(playerUid, xform) != station)
                continue;

            candidates.Add(playerUid);
        }

        var minPlayers =
            (int) Math.Ceiling(candidates.Count * (double) component.MinPlayerFraction);

        var maxPlayers =
            (int) Math.Floor(candidates.Count * (double) component.MaxPlayerFraction);

        if (maxPlayers < minPlayers || maxPlayers == 0)
        {
            Log.Info(
                $"Crew monitoring malfunction: no valid selection size " +
                $"for {candidates.Count} living players.");

            return;
        }

        var playersToSelect = RobustRandom.Next(minPlayers, maxPlayers + 1);
        var criticalLimit = component.CriticalChance;
        var deadLimit = criticalLimit + component.DeadChance;
        var poorLimit = deadLimit + component.PoorChance;

        for (var i = 0; i < playersToSelect; i++)
        {
            var playerUid = RobustRandom.PickAndTake(candidates);
            var roll = RobustRandom.Next(100);

            CrewMonitoringFakeState fakeState;

            if (roll < criticalLimit)
                fakeState = CrewMonitoringFakeState.Critical;
            else if (roll < deadLimit)
                fakeState = CrewMonitoringFakeState.Dead;
            else if (roll < poorLimit)
                fakeState = CrewMonitoringFakeState.Poor;
            else
                continue;

            var spoof = EnsureComp<CrewMonitoringSpoofComponent>(playerUid);

            spoof.Rule = uid;
            spoof.FakeState = fakeState;
            spoof.EndTime = stationEvent.EndTime.Value;

            component.AffectedPlayers.Add(playerUid);
        }

        Log.Info(
            $"Crew monitoring malfunction: station={station}, " +
            $"selected={playersToSelect}, " +
            $"spoofed={component.AffectedPlayers.Count}.");
    }

    protected override void Ended(
        EntityUid uid,
        CrewMonitoringMalfRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        foreach (var playerUid in component.AffectedPlayers)
        {

            

            if (!TryComp<CrewMonitoringSpoofComponent>(playerUid, out var spoof))
                continue;
            // Не удаляем подмену, если она уже принадлежит другому, более позднему событию.
            if (spoof.Rule != uid)
                continue;

            RemComp<CrewMonitoringSpoofComponent>(playerUid);
        }

        component.AffectedPlayers.Clear();
    }

    public void ApplySpoof(EntityUid playerUid, SuitSensorStatus status)
    {
        if (!TryComp<CrewMonitoringSpoofComponent>(playerUid, out var spoof))
            return;

        // Подмена только при существующем активном правиле ивента и до времени окончания.
        if (!TryComp<CrewMonitoringMalfRuleComponent>(spoof.Rule, out _) ||
            !TryComp<GameRuleComponent>(spoof.Rule, out var gameRule) ||
            !GameTicker.IsGameRuleActive(spoof.Rule, gameRule) ||
            Timing.CurTime >= spoof.EndTime)
        {
            RemComp<CrewMonitoringSpoofComponent>(playerUid);
            return;
        }

        switch (spoof.FakeState)
        {
            case CrewMonitoringFakeState.Dead:
                status.IsAlive = false;
                break;

            case CrewMonitoringFakeState.Critical:
                if (status.TotalDamage == null)
                    return;

                status.IsAlive = true;
                status.TotalDamageThreshold = 100;
                status.TotalDamage = 150;
                break;

            case CrewMonitoringFakeState.Poor:
                if (status.TotalDamage == null)
                    return;
                // Если персонаж реально умер после начала события, событие до окончания эффекта всё равно будет показывать его живым.
                status.IsAlive = true;
                status.TotalDamageThreshold = 100;
                status.TotalDamage = 75;
                break;
        }
    }
}
