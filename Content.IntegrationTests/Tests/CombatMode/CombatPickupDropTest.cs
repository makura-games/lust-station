using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CombatMode;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.CombatMode;

[TestFixture]
public sealed class CombatPickupDropTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, DummyTicker = false };

    [Test]
    public async Task CombatPreferenceBlocksVoluntaryPickupAndDropOnServer()
    {
        var map = await Pair.CreateTestMap();
        var user = ServerSession!.AttachedEntity!.Value;
        EntityUid held = default;
        EntityUid ground = default;
        await Server.WaitPost(() =>
        {
            Server.System<SharedTransformSystem>().SetCoordinates(user, map.GridCoords);
            Server.System<SharedCombatModeSystem>().SetInCombatMode(user, false);
            held = SEntMan.SpawnAtPosition("Screwdriver", map.GridCoords);
            ground = SEntMan.SpawnAtPosition("Crowbar", map.GridCoords);
            Assert.That(Server.System<SharedHandsSystem>().TryPickupAnyHand(user, held), Is.True);
        });

        await OverrideCVar(Side.Client, SunriseCCVars.CombatModeBlockPickup, true);
        await OverrideCVar(Side.Client, SunriseCCVars.CombatModeBlockDrop, true);
        await Server.WaitPost(() =>
        {
            Server.System<SharedCombatModeSystem>().SetInCombatMode(user, true);
            var hands = Server.System<SharedHandsSystem>();
            Assert.That(hands.TryDrop(user, held), Is.False, "Combat mode must prevent voluntary dropping on the server");
            Assert.That(hands.IsHolding(user, held), Is.True);
            Assert.That(hands.TryPickupAnyHand(user, ground), Is.False, "Combat mode must prevent picking up");

            Assert.That(hands.TryDrop(user, held, checkActionBlocker: false), Is.True,
                "Forced drops must remain possible");
            Assert.That(hands.TryPickupAnyHand(user, ground), Is.False,
                "Picking up must remain blocked with a free hand");
        });

        await OverrideCVar(Side.Client, SunriseCCVars.CombatModeBlockPickup, false);
        await Server.WaitPost(() =>
        {
            var hands = Server.System<SharedHandsSystem>();
            Assert.That(hands.TryPickupAnyHand(user, ground), Is.True);
            Assert.That(hands.TryDrop(user, ground), Is.False,
                "Pickup and drop preferences must be independent");
        });

        await OverrideCVar(Side.Client, SunriseCCVars.CombatModeBlockPickup, true);
        await OverrideCVar(Side.Client, SunriseCCVars.CombatModeBlockDrop, false);
        await Server.WaitPost(() =>
        {
            var hands = Server.System<SharedHandsSystem>();
            var item = SEntMan.SpawnAtPosition("Screwdriver", map.GridCoords);
            Assert.That(hands.TryPickupAnyHand(user, item), Is.False,
                "Pickup must remain blocked when only the drop preference is disabled");
            Assert.That(hands.TryDrop(user, ground), Is.True);
        });

        await OverrideCVar(Side.Client, SunriseCCVars.CombatModeBlockPickup, false);
        await Server.WaitPost(() =>
        {
            var hands = Server.System<SharedHandsSystem>();
            Assert.That(hands.TryPickupAnyHand(user, ground), Is.True);
            Assert.That(hands.TryDrop(user, ground), Is.True);
        });

        await Server.WaitPost(() =>
        {
            Server.System<SharedCombatModeSystem>().SetInCombatMode(user, false);
            var item = SEntMan.SpawnAtPosition("Screwdriver", map.GridCoords);
            var hands = Server.System<SharedHandsSystem>();
            Assert.That(hands.TryPickupAnyHand(user, item), Is.True);
            Assert.That(hands.TryDrop(user, item), Is.True);
        });
    }
}
