using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.CombatMode;

[TestFixture]
public sealed class ClickPlacementTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, DummyTicker = false };

    [Test]
    public async Task PreferenceBlocksClicksButAllowsExplicitStorageInsertion()
    {
        var map = await Pair.CreateTestMap();
        var user = ServerSession!.AttachedEntity!.Value;
        EntityUid bag = default;
        EntityUid table = default;
        EntityUid item = default;

        await Server.WaitPost(() =>
        {
            Server.System<SharedTransformSystem>().SetCoordinates(user, map.GridCoords);
            bag = SEntMan.SpawnAtPosition("ClothingBackpack", map.GridCoords);
            table = SEntMan.SpawnAtPosition("Table", map.GridCoords);
            item = SEntMan.SpawnAtPosition("Screwdriver", map.GridCoords);
            Assert.That(Server.System<SharedHandsSystem>().TryPickupAnyHand(user, item), Is.True);
        });

        await OverrideCVar(Side.Client, SunriseCCVars.BlockClickPlacement, true);
        await Server.WaitPost(() =>
        {
            var click = new InteractUsingEvent(user, item, bag, map.GridCoords);
            SEntMan.EventBus.RaiseLocalEvent(bag, click);
            Assert.That(Server.System<SharedHandsSystem>().IsHolding(user, item), Is.True,
                "Clicking storage must leave the item in hand");
            Assert.That(click.Handled, Is.True);

            var surfaceClick = new AfterInteractUsingEvent(user, item, table, map.GridCoords, true);
            SEntMan.EventBus.RaiseLocalEvent(table, surfaceClick);
            Assert.That(Server.System<SharedHandsSystem>().IsHolding(user, item), Is.True,
                "Clicking a surface must leave the item in hand");

            Assert.That(Server.System<SharedStorageSystem>().PlayerInsertHeldEntity(bag, user), Is.True,
                "Explicit storage insertion must still work");
            Assert.That(Server.System<SharedHandsSystem>().IsHolding(user, item), Is.False);
        });

        await OverrideCVar(Side.Client, SunriseCCVars.BlockClickPlacement, false);
        await Server.WaitPost(() =>
        {
            item = SEntMan.SpawnAtPosition("Screwdriver", map.GridCoords);
            var hands = Server.System<SharedHandsSystem>();
            Assert.That(hands.TryPickupAnyHand(user, item), Is.True);
            SEntMan.EventBus.RaiseLocalEvent(bag, new InteractUsingEvent(user, item, bag, map.GridCoords));
            Assert.That(hands.IsHolding(user, item), Is.False);

            item = SEntMan.SpawnAtPosition("Screwdriver", map.GridCoords);
            Assert.That(hands.TryPickupAnyHand(user, item), Is.True);
            SEntMan.EventBus.RaiseLocalEvent(table, new AfterInteractUsingEvent(user, item, table, map.GridCoords, true));
            Assert.That(hands.IsHolding(user, item), Is.False);
        });
    }
}
