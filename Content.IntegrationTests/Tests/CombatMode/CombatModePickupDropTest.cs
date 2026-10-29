using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Item;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.CombatMode;

[TestFixture]
public sealed class CombatModePickupDropTest : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings { Connected = true, DummyTicker = false };

    [Test]
    public async Task PickupAndDropRespectTheClientPreferenceAndCombatMode()
    {
        var client = Client;
        var pair = Pair;
        await pair.RunTicksSync(2);

        var attachedEntity = client.Session?.AttachedEntity;
        Assert.That(attachedEntity, Is.Not.Null);
        var localPlayer = attachedEntity!.Value;
        var results = new List<(bool PickupPreference, bool DropPreference, bool Combat, bool Pickup, bool Drop)>();
        var cases = new[]
        {
            (PickupPreference: true, DropPreference: true, Combat: true),
            (PickupPreference: false, DropPreference: true, Combat: true),
            (PickupPreference: true, DropPreference: false, Combat: true),
            (PickupPreference: true, DropPreference: true, Combat: false),
        };

        await client.WaitPost(() =>
        {
            var config = client.ResolveDependency<IConfigurationManager>();
            var combatSystem = client.System<SharedCombatModeSystem>();
            var combatMode = client.EntMan.GetComponent<CombatModeComponent>(localPlayer);

            foreach (var testCase in cases)
            {
                config.SetCVar(SunriseCCVars.CombatModeBlockPickup, testCase.PickupPreference);
                config.SetCVar(SunriseCCVars.CombatModeBlockDrop, testCase.DropPreference);
                combatSystem.SetInCombatMode(localPlayer, testCase.Combat, combatMode);

                var pickupAttempt = new PickupAttemptEvent(localPlayer, EntityUid.Invalid, false);
                client.EntMan.EventBus.RaiseLocalEvent(localPlayer, pickupAttempt);

                var dropAttempt = new DropAttemptEvent();
                client.EntMan.EventBus.RaiseLocalEvent(localPlayer, dropAttempt);

                results.Add((testCase.PickupPreference, testCase.DropPreference, testCase.Combat,
                    !pickupAttempt.Cancelled, !dropAttempt.Cancelled));
            }

            config.SetCVar(SunriseCCVars.CombatModeBlockPickup, true);
            config.SetCVar(SunriseCCVars.CombatModeBlockDrop, true);
            combatSystem.SetInCombatMode(localPlayer, false, combatMode);
        });

        Assert.That(results, Has.Count.EqualTo(cases.Length));
        foreach (var (pickupPreference, dropPreference, combat, pickup, drop) in results)
        {
            Assert.Multiple(() =>
            {
                Assert.That(pickup, Is.EqualTo(!(pickupPreference && combat)));
                Assert.That(drop, Is.EqualTo(!(dropPreference && combat)));
            });
        }
    }
}
