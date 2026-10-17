using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Radio;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class RadioChannelShuffleRuleSystem
    : StationEventSystem<RadioChannelShuffleRuleComponent>
{
    protected override void Started(
        EntityUid uid,
        RadioChannelShuffleRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.Station = null;

        if (component.RedirectChance < 0 ||
            component.RedirectChance > 1)
        {
            Log.Error("Radio channel shuffle: invalid redirect chance.");
            return;
        }

        // Проверяем ID каналов заранее.
        // Опечатка в YAML не должна приводить к падению при отправке.
        foreach (var channelId in component.Channels)
        {
            if (PrototypeManager.TryIndex<RadioChannelPrototype>(
                    channelId,
                    out _))
            {
                continue;
            }

            Log.Error(
                $"Radio channel shuffle: unknown channel '{channelId}'.");

            return;
        }

        if (!TryGetRandomStation(out var station))
            return;

        component.Station = station;

        Log.Info(
            $"Radio channel shuffle started: station={station}, " +
            $"chance={component.RedirectChance}.");
    }

    protected override void Ended(
        EntityUid uid,
        RadioChannelShuffleRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleEndedEvent args)
    {
        // Сначала отключаем эффект.
        component.Station = null;

        base.Ended(uid, component, gameRule, args);
    }

    public bool TryRedirect(
        EntityUid radioSource,
        RadioChannelPrototype originalChannel,
        out RadioChannelPrototype redirectedChannel)
    {
        redirectedChannel = originalChannel;

        // Определяем станцию источника передачи.
        // Для гарнитуры это её фактическая принадлежность станции.
        var sourceStation = StationSystem.GetOwningStation(radioSource);

        if (sourceStation == null)
            return false;

        var query =
            EntityQueryEnumerator<
                RadioChannelShuffleRuleComponent,
                GameRuleComponent,
                StationEventComponent>();

        while (query.MoveNext(
                   out var ruleUid,
                   out var component,
                   out var gameRule,
                   out var stationEvent))
        {
            // Не применяем добавленные, но ещё не начавшиеся правила.
            if (!GameTicker.IsGameRuleActive(ruleUid, gameRule))
                continue;

            if (component.Station != sourceStation)
                continue;

            // Дополнительная защита от применения истёкшего события
            // до обработки его завершения.
            if (stationEvent.EndTime == null ||
                Timing.CurTime >= stationEvent.EndTime.Value)
            {
                continue;
            }

            // Канал вне разрешённого списка не затрагиваем.
            if (!component.Channels.Contains(originalChannel.ID))
                continue;

            var destinations = new List<RadioChannelPrototype>();
            var seen = new HashSet<string>();

            foreach (var channelId in component.Channels)
            {
                // Исходный канал исключаем.
                // Дубликаты в настройках тоже исключаем.
                if (channelId == originalChannel.ID ||
                    !seen.Add(channelId))
                {
                    continue;
                }

                if (!PrototypeManager.TryIndex<RadioChannelPrototype>(
                        channelId,
                        out var prototype))
                {
                    continue;
                }

                destinations.Add(prototype);
            }

            if (destinations.Count == 0)
                return false;

            // Один бросок на сообщение. При нескольких одновременно активных событиях не складываем вероятность повторными бросками.
            if (!RobustRandom.Prob(component.RedirectChance))
                return false;

            redirectedChannel = RobustRandom.Pick(destinations);
            return true;
        }

        return false;
    }
}