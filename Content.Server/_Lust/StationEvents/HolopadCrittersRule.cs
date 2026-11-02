using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.Holopad;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// этот ивент спавнит выбраные ентити на голопадах, только на Lust
/// </summary>
public sealed class HolopadCrittersRule : StationEventSystem<HolopadCrittersRuleComponent>
{
    protected override void Started(EntityUid uid, HolopadCrittersRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
        {
            Log.Warning($"Unable to find a valid station for holopad critters event!");
            return;
        }

        var holopadQuery = EntityQueryEnumerator<HolopadComponent, TransformComponent>();
        var validHolopads = new List<EntityCoordinates>();

        while (holopadQuery.MoveNext(out _, out _, out var transform))
        {
            if (!transform.Anchored)
                continue;

            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == station)
            {
                validHolopads.Add(transform.Coordinates);
            }
        }

        if (validHolopads.Count == 0)
        {
            Log.Warning($"No valid holopads found for holopad critters event on station {station}!");
            return;
        }

        if (component.SpecialEntries.Count > 0)
        {
            List<EntityCoordinates> targetHolopads;

            if (component.SpawnFromAllHolopads)
            {
                targetHolopads = validHolopads;
            }
            else
            {
                var count = Math.Min(component.MaxHolopadsToSpawn, validHolopads.Count);
                targetHolopads = new List<EntityCoordinates>();

                for (int i = 0; i < count; i++)
                {
                    var holopad = RobustRandom.PickAndTake(validHolopads);
                    targetHolopads.Add(holopad);
                }
            }

            var specialSpawns = EntitySpawnCollection.GetSpawns(component.SpecialEntries, RobustRandom);

            EntityCoordinates? guaranteedHolopad = null;

            if (specialSpawns.Count > 0)
            {
                guaranteedHolopad = RobustRandom.Pick(targetHolopads);

                foreach (var spawn in specialSpawns)
                {
                    Spawn(spawn, guaranteedHolopad.Value);
                }
            }

            foreach (var holopad in targetHolopads)
            {
                if (guaranteedHolopad.HasValue && holopad.Equals(guaranteedHolopad.Value) && specialSpawns.Count > 0)
                    continue;

                foreach (var spawn in EntitySpawnCollection.GetSpawns(component.SpecialEntries, RobustRandom))
                {
                    Spawn(spawn, holopad);
                }
            }
        }

        if (component.Entries.Count > 0 && component.SpawnFromAllHolopads)
        {
            foreach (var holopad in validHolopads)
            {
                var regularSpawns = EntitySpawnCollection.GetSpawns(component.Entries, RobustRandom);

                foreach (var spawn in regularSpawns)
                {
                    Spawn(spawn, holopad);
                }
            }
        }

        Log.Info($"Holopad critters event started: {validHolopads.Count} holopads, {component.Entries.Count + component.SpecialEntries.Count} spawn entries.");
    }
}