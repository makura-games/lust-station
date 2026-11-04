using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Lust.DmArena;
using Content.Shared._Lust.DmArena;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Lust;

/// <summary>
/// Проверяет данные магазина ДМ арены и серверную проверку выбранного набора.
/// </summary>
[TestFixture]
public sealed class DmArenaStoreTests : GameTest
{
    private const string RuleId = "LustDmArenaRule";

    [Test]
    public async Task StoreDataIsConsistent()
    {
        await Server.WaitAssertion(() =>
        {
            var loc = Server.ResolveDependency<ILocalizationManager>();
            var rule = SEntMan.GetComponent<DmArenaRuleComponent>(SEntMan.Spawn(RuleId));

            Assert.That(SProtoMan.HasIndex(rule.Gear), "Base gear is missing");
            Assert.That(rule.ShopItems, Is.Not.Empty);

            var ammoTypes = rule.ShopItems.Where(x => x.AmmoType != null).Select(x => x.AmmoType).ToHashSet();

            foreach (var item in rule.ShopItems)
            {
                var id = item.Gear.Id;

                Assert.That(SProtoMan.HasIndex(item.Gear), $"{id}: gear prototype is missing");
                Assert.That(item.Cost, Is.GreaterThan(0), $"{id}: cost must be positive");
                Assert.That(item.Cost, Is.LessThanOrEqualTo(rule.Budget), $"{id}: costs more than the budget");
                Assert.That(item.MaxCount, Is.GreaterThanOrEqualTo(1), $"{id}: MaxCount must be at least 1");

                Assert.That(loc.TryGetString(item.Name, out _), $"{id}: no localization for {item.Name}");
                Assert.That(loc.TryGetString(item.Category, out _), $"{id}: no localization for {item.Category}");

                if (item.Description is { } description)
                    Assert.That(loc.TryGetString(description, out _), $"{id}: no localization for {description}");

                if (item.AmmoFor is { } ammoFor && ammoFor != "base")
                    Assert.That(ammoTypes, Does.Contain(ammoFor), $"{id}: no weapon provides ammo type {ammoFor}");

                var gear = SProtoMan.Index(item.Gear);
                Assert.That(gear.Inhand.Count, Is.LessThanOrEqualTo(2), $"{id}: more than two hands needed");
            }
        });
    }

    [Test]
    public async Task SetsAreUsable()
    {
        await Server.WaitAssertion(() =>
        {
            var loc = Server.ResolveDependency<ILocalizationManager>();
            var ruleUid = SEntMan.Spawn(RuleId);
            var rule = SEntMan.GetComponent<DmArenaRuleComponent>(ruleUid);

            foreach (var set in rule.Sets)
            {
                Assert.That(loc.TryGetString(set.Name, out _), $"No localization for set {set.Name}");

                var cost = 0;
                foreach (var gear in set.Items)
                {
                    var item = rule.ShopItems.Find(x => x.Gear == gear);
                    Assert.That(item, Is.Not.Null, $"{set.Name}: {gear} is not sold in the shop");
                    cost += item!.Cost;
                }

                Assert.That(cost, Is.LessThanOrEqualTo(rule.Budget), $"{set.Name}: set is over the budget");
            }

            var state = SEntMan.System<DmArenaMatchSystem>().GetLoadoutState(ruleUid, new NetUserId(Guid.NewGuid()));
            Assert.That(state.Sets, Has.Count.EqualTo(rule.Sets.Count));
        });
    }

    [Test]
    public async Task PresetValidation()
    {
        await Server.WaitAssertion(() =>
        {
            var arena = SEntMan.System<DmArenaMatchSystem>();
            var ruleUid = SEntMan.Spawn(RuleId);
            var rule = SEntMan.GetComponent<DmArenaRuleComponent>(ruleUid);
            var user = new NetUserId(Guid.NewGuid());

            bool Saved(List<int> items)
            {
                arena.SavePreset(ruleUid, user, items);
                var preset = arena.GetLoadoutState(ruleUid, user).Preset;
                return preset != null && preset.SequenceEqual(items);
            }

            var plain = rule.ShopItems.FindIndex(x => x.AmmoFor is null or "base");
            Assert.That(plain, Is.GreaterThanOrEqualTo(0));
            Assert.That(Saved(new List<int> { plain }), "A single plain item should be accepted");

            Assert.That(Saved(new List<int> { -1 }), Is.False, "Negative index");
            Assert.That(Saved(new List<int> { rule.ShopItems.Count }), Is.False, "Index past the end");

            var single = rule.ShopItems.FindIndex(x => x.MaxCount == 1);
            Assert.That(single, Is.GreaterThanOrEqualTo(0));
            Assert.That(Saved(new List<int> { single, single }), Is.False, "Duplicate of a single item");

            Assert.That(Saved(Enumerable.Repeat(plain, rule.Budget + 1).ToList()), Is.False, "List longer than the budget");

            var ammo = rule.ShopItems.FindIndex(x => x.AmmoFor is { } f && f != "base");
            if (ammo >= 0)
                Assert.That(Saved(new List<int> { ammo }), Is.False, "Ammo without a weapon");

            for (var a = 0; a < rule.ShopItems.Count; a++)
            {
                var slotsA = SProtoMan.Index(rule.ShopItems[a].Gear).Equipment.Keys.ToHashSet();
                for (var b = a + 1; b < rule.ShopItems.Count; b++)
                {
                    if (!SProtoMan.Index(rule.ShopItems[b].Gear).Equipment.Keys.Any(slotsA.Contains))
                        continue;

                    Assert.That(Saved(new List<int> { a, b }), Is.False, $"Items {a} and {b} share a slot");
                    return;
                }
            }
        });
    }
}
