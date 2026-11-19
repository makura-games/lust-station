#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Lust.Swarmer;
using Content.Shared._Lust.Swarmer.Components;
using Content.Shared.Emp;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._Lust.Swarmer;

public sealed class SwarmerTest : GameTest
{
    private static readonly Robust.Shared.Prototypes.ProtoId<Content.Shared.Damage.Prototypes.DamageTypePrototype> BluntDamage = "Blunt";

    [Test]
    public async Task EmpKillsSwarmerAndDropsLoot()
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();

        EntityUid swarmer = default;

        await server.WaitPost(() =>
        {
            swarmer = entMan.SpawnAtPosition("MobSwarmer", testMap.GridCoords);
        });

        await server.WaitPost(() =>
        {
            entMan.System<SharedEmpSystem>().DoEmpEffects(swarmer, 50000, TimeSpan.FromSeconds(10));
        });

        // Несколько тиков, чтобы отработали отложенное удаление (QueueDel) и спавн лута/эффектов.
        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(swarmer), Is.True, "Swarmer should be destroyed by EMP");

            var loot = entMan.System<EntityLookupSystem>()
                .GetEntitiesInRange(testMap.GridCoords, 2, LookupFlags.All | LookupFlags.Approximate)
                .Select(e => entMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID)
                .ToList();

            Assert.That(loot, Does.Contain("SheetSteel1"), $"Found: {string.Join(", ", loot)}");
        });
    }

    [Test]
    public async Task ClickIntegratesOnlyValidItems()
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();

        EntityUid swarmer = default;
        EntityUid steel = default;
        EntityUid glass = default;
        EntityUid gun = default;
        EntityUid banana = default;

        await server.WaitPost(() =>
        {
            var coords = testMap.GridCoords;
            swarmer = entMan.SpawnAtPosition("MobSwarmer", coords);
            steel = entMan.SpawnAtPosition("SheetSteel", coords);
            glass = entMan.SpawnAtPosition("SheetGlass1", coords);
            gun = entMan.SpawnAtPosition("WeaponPistolMk58", coords);
            banana = entMan.SpawnAtPosition("FoodBanana", coords);
        });

        await server.WaitPost(() =>
        {
            var interaction = entMan.System<SharedInteractionSystem>();
            interaction.InteractHand(swarmer, steel);
            interaction.InteractHand(swarmer, glass);
            interaction.InteractHand(swarmer, gun);
            interaction.InteractHand(swarmer, banana);
        });

        // Несколько тиков, чтобы отработали отложенное удаление (QueueDel) и спавн лута/эффектов.
        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<SwarmerComponent>(swarmer);
            Assert.Multiple(() =>
            {
                // На старте 20, +1 за лист стали, +1 за лист стекла.
                Assert.That(comp.Resources, Is.EqualTo(22));
                Assert.That(entMan.GetComponent<StackComponent>(steel).Count, Is.EqualTo(29), "Only one sheet should be taken from the stack");
                Assert.That(entMan.Deleted(glass), Is.True, "A single glass sheet should be consumed entirely");
                Assert.That(entMan.Deleted(gun), Is.False, "Guns can't be integrated");
                Assert.That(entMan.Deleted(banana), Is.False, "Bananas have no steel or glass");
            });
        });
    }

    /// <summary>
    /// Spawns a swarmer and a target next to each other, makes the swarmer click the target and waits.
    /// </summary>
    private async Task<(EntityUid Target, List<string?> Nearby)> ClickAndWait(string targetProto, float seconds, string? inside = null)
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();

        EntityUid swarmer = default;
        EntityUid target = default;
        var inserted = true;

        await server.WaitPost(() =>
        {
            // Свармер стоит на соседней клетке: на одной клетке с твёрдой целью его вытолкнет и do-after прервётся.
            var mapSys = entMan.System<SharedMapSystem>();
            mapSys.SetTile(testMap.Grid.Owner, testMap.Grid.Comp, new Vector2i(1, 0), testMap.Tile.Tile);

            var targetCoords = new EntityCoordinates(testMap.Grid, 0.5f, 0.5f);
            swarmer = entMan.SpawnAtPosition("MobSwarmer", new EntityCoordinates(testMap.Grid, 1.5f, 0.5f));
            target = entMan.SpawnAtPosition(targetProto, targetCoords);

            if (inside != null)
            {
                var content = entMan.SpawnAtPosition(inside, targetCoords);
                var storage = entMan.System<Content.Shared.Storage.EntitySystems.SharedEntityStorageSystem>();
                inserted = storage.Insert(content, target);
            }

            entMan.System<SharedInteractionSystem>().InteractHand(swarmer, target);
        });

        Assert.That(inserted, Is.True, $"Failed to put {inside} into {targetProto}");

        await RunSeconds(seconds);

        var nearby = new List<string?>();
        await server.WaitPost(() =>
        {
            nearby = entMan.System<EntityLookupSystem>()
                .GetEntitiesInRange(testMap.GridCoords, 2, LookupFlags.All | LookupFlags.Approximate)
                .Select(e => entMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID)
                .ToList();
        });

        return (target, nearby);
    }

    [Test]
    public async Task DismantlesMachineIntoParts()
    {
        var (target, nearby) = await ClickAndWait("Autolathe", 11);
        var entMan = Pair.Server.ResolveDependency<IEntityManager>();

        Assert.Multiple(() =>
        {
            Assert.That(entMan.Deleted(target), Is.True, "Autolathe should be dismantled");
            Assert.That(nearby, Does.Contain("AutolatheMachineCircuitboard"), $"Found: {string.Join(", ", nearby)}");
            Assert.That(nearby, Does.Contain("SheetSteel1"), $"Found: {string.Join(", ", nearby)}");
        });
    }

    [TestCase("Chair")]
    [TestCase("ComfyChair")]
    [TestCase("ChairOfficeDark")]
    public async Task SeatGivesBackSteel(string proto)
    {
        var (target, nearby) = await ClickAndWait(proto, 2);

        Assert.Multiple(() =>
        {
            Assert.That(Pair.Server.ResolveDependency<IEntityManager>().Deleted(target), Is.True);
            Assert.That(nearby, Does.Contain("SheetSteel1"), $"Found: {string.Join(", ", nearby)}");
        });
    }

    [Test]
    public async Task TableBreaksIntoMaterial()
    {
        var (_, nearby) = await ClickAndWait("Table", 2);

        Assert.Multiple(() =>
        {
            Assert.That(nearby, Does.Not.Contain("Table"), $"Found: {string.Join(", ", nearby)}");
            Assert.That(nearby, Does.Contain("SheetSteel1"), $"Found: {string.Join(", ", nearby)}");
        });
    }

    [Test]
    public async Task TableFrameBreaksIntoRod()
    {
        var (target, nearby) = await ClickAndWait("TableFrame", 2);

        Assert.Multiple(() =>
        {
            Assert.That(Pair.Server.ResolveDependency<IEntityManager>().Deleted(target), Is.True);
            Assert.That(nearby, Does.Contain("PartRodMetal1"), $"Found: {string.Join(", ", nearby)}");
        });
    }

    [Test]
    public async Task LockerDropsItsContents()
    {
        var (target, nearby) = await ClickAndWait("LockerSteel", 2, inside: "Crowbar");

        Assert.Multiple(() =>
        {
            Assert.That(Pair.Server.ResolveDependency<IEntityManager>().Deleted(target), Is.True);
            Assert.That(nearby, Does.Contain("Crowbar"), $"Found: {string.Join(", ", nearby)}");
        });
    }

    [Test]
    public async Task GirderBreaksIntoSheet()
    {
        var (target, nearby) = await ClickAndWait("Girder", 2);

        Assert.Multiple(() =>
        {
            Assert.That(Pair.Server.ResolveDependency<IEntityManager>().Deleted(target), Is.True);
            Assert.That(nearby, Does.Contain("SheetSteel1"), $"Found: {string.Join(", ", nearby)}");
        });
    }

    [Test]
    public async Task ReinforcedGirderBreaksIntoPlasteel()
    {
        var (_, nearby) = await ClickAndWait("ReinforcedGirder", 2);

        Assert.Multiple(() =>
        {
            Assert.That(nearby, Does.Not.Contain("ReinforcedGirder"), $"Found: {string.Join(", ", nearby)}");
            Assert.That(nearby, Does.Contain("SheetPlasteel1"), $"Found: {string.Join(", ", nearby)}");
        });
    }

    [Test]
    public async Task AbilitiesWork()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        EntityUid swarmer = default;

        await server.WaitPost(() =>
        {
            swarmer = entMan.SpawnAtPosition("MobSwarmer", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
        });

        // Барьер: секунда, 4 ресурса.
        await server.WaitPost(() => entMan.EventBus.RaiseLocalEvent(swarmer, new SwarmerBarricadeActionEvent()));
        await RunSeconds(1.5f);

        // Вторую постройку на той же клетке ставить нельзя.
        await server.WaitPost(() => entMan.EventBus.RaiseLocalEvent(swarmer, new SwarmerTrapActionEvent()));
        await RunSeconds(0.5f);

        await server.WaitAssertion(() =>
        {
            var lookup = entMan.System<EntityLookupSystem>();
            var structures = lookup.GetEntitiesInRange<SwarmerStructureComponent>(new EntityCoordinates(testMap.Grid, 0.5f, 0.5f), 0.4f);
            Assert.Multiple(() =>
            {
                Assert.That(structures, Has.Count.EqualTo(1));
                Assert.That(entMan.GetComponent<MetaDataComponent>(structures.First()).EntityPrototype?.ID, Is.EqualTo("SwarmerBarricade"));
                Assert.That(entMan.GetComponent<SwarmerComponent>(swarmer).Resources, Is.EqualTo(16));
            });
        });

        // Саморемонт: 10 секунд на месте лечат всё.
        await server.WaitPost(() =>
        {
            var damage = new Content.Shared.Damage.DamageSpecifier(
                server.ResolveDependency<Robust.Shared.Prototypes.IPrototypeManager>().Index(BluntDamage), 20);
            entMan.System<Content.Shared.Damage.Systems.DamageableSystem>().TryChangeDamage(swarmer, damage, true);
            entMan.EventBus.RaiseLocalEvent(swarmer, new SwarmerSelfRepairActionEvent());
        });
        await RunSeconds(11);

        // Переключение света.
        var lightWasOn = false;
        await server.WaitPost(() =>
        {
            var light = entMan.System<SharedPointLightSystem>();
            light.TryGetLight(swarmer, out var comp);
            lightWasOn = comp!.Enabled;
            entMan.EventBus.RaiseLocalEvent(swarmer, new SwarmerToggleLightActionEvent());
        });

        await server.WaitAssertion(() =>
        {
            var light = entMan.System<SharedPointLightSystem>();
            light.TryGetLight(swarmer, out var comp);
            Assert.Multiple(() =>
            {
#pragma warning disable CS0618 // Fine for a test: we only check that everything was healed.
                Assert.That(entMan.System<Content.Shared.Damage.Systems.DamageableSystem>().GetTotalDamage(swarmer).Float(), Is.EqualTo(0));
#pragma warning restore CS0618
                Assert.That(comp!.Enabled, Is.Not.EqualTo(lightWasOn));
            });
        });
    }

    [Test]
    public async Task TrapIsPlacedAtClickedSpot()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        EntityUid swarmer = default;
        var target = new EntityCoordinates(testMap.Grid, 1.3f, 0.7f);

        await server.WaitPost(() =>
        {
            entMan.System<SharedMapSystem>().SetTile(testMap.Grid.Owner, testMap.Grid.Comp, new Vector2i(1, 0), testMap.Tile.Tile);
            swarmer = entMan.SpawnAtPosition("MobSwarmer", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
            entMan.EventBus.RaiseLocalEvent(swarmer, new SwarmerTrapActionEvent { Target = target });
        });

        await RunSeconds(0.5f);

        await server.WaitAssertion(() =>
        {
            var lookup = entMan.System<EntityLookupSystem>();
            var traps = lookup.GetEntitiesInRange<SwarmerTrapComponent>(target, 0.5f);
            Assert.Multiple(() =>
            {
                // Ловушка встаёт мгновенно, ровно в точку клика (не в центр клетки), прикреплена к станции и без поворота.
                Assert.That(traps, Has.Count.EqualTo(1));
                var trapXform = entMan.GetComponent<TransformComponent>(traps.First());
                Assert.That(trapXform.ParentUid, Is.EqualTo(testMap.Grid.Owner));
                Assert.That(trapXform.LocalPosition.X, Is.EqualTo(1.3f).Within(0.01f));
                Assert.That(trapXform.LocalPosition.Y, Is.EqualTo(0.7f).Within(0.01f));
                Assert.That(entMan.GetComponent<TransformComponent>(traps.First()).LocalRotation, Is.EqualTo(Angle.Zero));
                Assert.That(entMan.GetComponent<SwarmerComponent>(swarmer).Resources, Is.EqualTo(16));
            });
        });
    }

    [Test]
    public async Task HitsAndTrapDrainBorgBattery()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var swarmerSystem = entMan.System<Content.Server._Lust.Swarmer.SwarmerSystem>();
        var powerCell = entMan.System<Content.Shared.PowerCell.PowerCellSystem>();
        var battery = entMan.System<Content.Shared.Power.EntitySystems.SharedBatterySystem>();
        EntityUid borg = default;
        float maxCharge = 0;
        var drained = false;
        float chargeAfterHit = 0;

        await server.WaitPost(() =>
        {
            var gravity = entMan.EnsureComponent<Content.Shared.Gravity.GravityComponent>(testMap.Grid);
            gravity.Enabled = true;
            entMan.Dirty(testMap.Grid, gravity);

            borg = entMan.SpawnAtPosition("PlayerBorgBattery", new EntityCoordinates(testMap.Grid, 2.5f, 0.5f));
            powerCell.TryGetBatteryFromSlot(borg, out var cell);
            maxCharge = cell!.Value.Comp.MaxCharge;

            // Удар роевика отнимает долю заряда.
            drained = swarmerSystem.TryDrainBorg(borg, 0.2f);
            chargeAfterHit = battery.GetCharge(cell.Value.AsNullable());
            battery.SetCharge(cell.Value.AsNullable(), maxCharge);

            entMan.SpawnAtPosition("SwarmerTrap", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
            entMan.System<SharedTransformSystem>().SetCoordinates(borg, new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
        });

        await RunSeconds(0.5f);

        await server.WaitAssertion(() =>
        {
            Assert.That(drained);
            Assert.That(chargeAfterHit, Is.EqualTo(maxCharge * 0.8f).Within(0.01f));
            Assert.That(powerCell.TryGetBatteryFromSlot(borg, out var cell));
            Assert.That(battery.GetCharge(cell!.Value.AsNullable()), Is.EqualTo(maxCharge * 0.5f).Within(maxCharge * 0.02f));
            Assert.That(entMan.HasComponent<Content.Shared.Stunnable.StunnedComponent>(borg));
        });

        await RunSeconds(2);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.HasComponent<Content.Shared.Stunnable.StunnedComponent>(borg), Is.False);
        });
    }

    [TestCase("MobMouse", true)]
    [TestCase("MobSwarmer", false)]
    public async Task TrapTriggersOnlyOnNonSwarmers(string victimProto, bool shouldTrigger)
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        EntityUid trap = default;

        await server.WaitPost(() =>
        {
            // Ловушки не срабатывают на мобов в невесомости, а на тестовой карте по умолчанию нет гравитации.
            var gravity = entMan.EnsureComponent<Content.Shared.Gravity.GravityComponent>(testMap.Grid);
            gravity.Enabled = true;
            entMan.Dirty(testMap.Grid, gravity);

            trap = entMan.SpawnAtPosition("SwarmerTrap", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
            entMan.SpawnAtPosition(victimProto, new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
        });

        await RunSeconds(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(trap), Is.EqualTo(shouldTrigger));
        });
    }

    [Test]
    public async Task TeleportRestrainsAndMovesVictim()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        EntityUid swarmer = default;
        EntityUid victim = default;

        await server.WaitPost(() =>
        {
            // Пол побольше, чтобы было куда телепортировать.
            var mapSys = entMan.System<SharedMapSystem>();
            for (var x = -5; x <= 5; x++)
            {
                for (var y = -5; y <= 5; y++)
                {
                    mapSys.SetTile(testMap.Grid.Owner, testMap.Grid.Comp, new Vector2i(x, y), testMap.Tile.Tile);
                }
            }

            // Телепорт выбирает только клетки с пригодным воздухом.
            server.ResolveDependency<Robust.Server.Console.IServerConsoleHost>()
                .ExecuteCommand($"fixgridatmos {entMan.GetNetEntity(testMap.Grid.Owner)}");
        });

        await RunSeconds(1);

        await server.WaitPost(() =>
        {
            swarmer = entMan.SpawnAtPosition("MobSwarmer", new EntityCoordinates(testMap.Grid, 1.5f, 0.5f));
            victim = entMan.SpawnAtPosition("MobHuman", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
            entMan.EventBus.RaiseLocalEvent(swarmer, new SwarmerTeleportActionEvent { Target = victim });
        });

        await RunSeconds(4);

        await server.WaitAssertion(() =>
        {
            var cuffs = entMan.System<Content.Shared.Cuffs.SharedCuffableSystem>();
            var pos = entMan.GetComponent<TransformComponent>(victim).LocalPosition;
            Assert.Multiple(() =>
            {
                Assert.That(cuffs.IsCuffed(victim), Is.True, "Victim should be restrained");
                Assert.That(entMan.System<SharedPointLightSystem>().TryGetLight(victim, out _), Is.True, "Restrained victim should glow");
                Assert.That(pos, Is.Not.EqualTo(new System.Numerics.Vector2(0.5f, 0.5f)), "Victim should be moved");
            });
        });

        // Снятые энергонаручники исчезают.
        EntityUid energyCuffs = default;
        await server.WaitPost(() =>
        {
            var cuffable = entMan.GetComponent<Content.Shared.Cuffs.Components.CuffableComponent>(victim);
            energyCuffs = cuffable.Container.ContainedEntities[^1];
            entMan.System<Content.Shared.Cuffs.SharedCuffableSystem>().Uncuff(victim, victim, energyCuffs);
        });

        await RunSeconds(0.5f);

        await server.WaitAssertion(() =>
        {
            var cuffs = entMan.System<Content.Shared.Cuffs.SharedCuffableSystem>();
            Assert.Multiple(() =>
            {
                Assert.That(cuffs.IsCuffed(victim), Is.False, "Victim should be free");
                Assert.That(entMan.Deleted(energyCuffs), Is.True, "Energy cuffs should vanish");
                Assert.That(entMan.System<SharedPointLightSystem>().TryGetLight(victim, out _), Is.False, "Glow should go away with the cuffs");
            });
        });
    }

    [Test]
    public async Task ReplicateMakesDrone()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        EntityUid swarmer = default;

        await server.WaitPost(() =>
        {
            swarmer = entMan.SpawnAtPosition("MobSwarmer", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
            // Репликация стоит полный запас ресурсов.
            entMan.GetComponent<SwarmerComponent>(swarmer).Resources = 100;
            entMan.EventBus.RaiseLocalEvent(swarmer, new SwarmerReplicateActionEvent());
        });

        await RunSeconds(6);

        List<EntityUid> Swarmers()
        {
            var list = new List<EntityUid>();
            var query = entMan.EntityQueryEnumerator<SwarmerComponent>();
            while (query.MoveNext(out var uid, out _))
                list.Add(uid);
            return list;
        }

        await server.WaitAssertion(() =>
        {
            var swarmers = Swarmers();
            Assert.That(swarmers, Has.Count.EqualTo(2));

            var replica = swarmers.Single(s => s != swarmer);
            var appearance = entMan.System<SharedAppearanceSystem>();
            Assert.Multiple(() =>
            {
                Assert.That(entMan.GetComponent<SwarmerComponent>(swarmer).Resources, Is.EqualTo(0), "Replication costs 100");
                Assert.That(entMan.GetComponent<MetaDataComponent>(replica).EntityPrototype?.ID, Is.EqualTo("MobSwarmer"));
                Assert.That(entMan.GetComponent<SwarmerComponent>(replica).Resources, Is.EqualTo(0), "Replicas start empty");
                Assert.That(entMan.HasComponent<Content.Server.NPC.HTN.HTNComponent>(replica), Is.False, "Replicas have no AI");
                Assert.That(entMan.HasComponent<Content.Server.Ghost.Roles.Components.GhostTakeoverAvailableComponent>(replica), Is.True, "Replicas wait for a ghost");
                Assert.That(appearance.TryGetData<bool>(replica, SwarmerVisuals.Active, out var active) && !active, Is.True, "Replicas start dormant");
            });
        });

        // На вторую копию ресурсов не хватает.
        await server.WaitPost(() => entMan.EventBus.RaiseLocalEvent(swarmer, new SwarmerReplicateActionEvent()));
        await RunSeconds(6);
        await server.WaitAssertion(() =>
        {
            Assert.That(Swarmers(), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task SwarmerSpeaksOnlyInSounds()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var loc = server.ResolveDependency<Robust.Shared.Localization.ILocalizationManager>();
        EntityUid swarmer = default;
        var spoken = string.Empty;

        await server.WaitPost(() =>
        {
            swarmer = entMan.SpawnAtPosition("MobSwarmer", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
            spoken = entMan.System<Content.Server.Chat.Systems.ChatSystem>().TransformSpeech(swarmer, "Отдайте нам станцию.");
        });

        await server.WaitAssertion(() =>
        {
            var phrases = new[] { "swarmer-speech-buzz", "swarmer-speech-whoop", "swarmer-speech-beep" }
                .Select(id => loc.GetString(id))
                .ToList();

            Assert.Multiple(() =>
            {
                // Вслух любая фраза превращается в один из звуков, а связь роя идёт через коллективный разум.
                Assert.That(phrases, Does.Contain(spoken));
                Assert.That(entMan.GetComponent<Content.Shared._Sunrise.CollectiveMind.CollectiveMindComponent>(swarmer).Minds, Does.Contain("Swarmer"));
                Assert.That(entMan.GetComponent<Content.Shared.Speech.SpeechComponent>(swarmer).SpeechSounds, Is.Null);
            });
        });
    }

    [Test]
    public async Task SwarmerCanEnterVents()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var inserted = false;

        await server.WaitPost(() =>
        {
            var coords = new EntityCoordinates(testMap.Grid, 0.5f, 0.5f);
            var swarmer = entMan.SpawnAtPosition("MobSwarmer", coords);
            var holder = entMan.SpawnAtPosition(Content.Shared._Sunrise.VentCraw.Components.VentCrawEntryComponent.HolderPrototypeId, coords);
            var holderComp = entMan.GetComponent<Content.Shared._Sunrise.VentCraw.Components.VentCrawHolderComponent>(holder);
            inserted = entMan.System<Content.Server._Sunrise.VentCraw.VentCrawTubeSystem>().TryInsert(holder, swarmer, holderComp);
        });

        await server.WaitAssertion(() =>
        {
            // Вентиляция пускает только существ с телом, поэтому у роевика пустое тело, как у боргов.
            Assert.That(inserted);
        });
    }

    [Test]
    public async Task EscapeJumpMovesSwarmer()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        EntityUid swarmer = default;

        await server.WaitPost(() =>
        {
            var mapSys = entMan.System<SharedMapSystem>();
            for (var x = -5; x <= 5; x++)
            {
                for (var y = -5; y <= 5; y++)
                {
                    mapSys.SetTile(testMap.Grid.Owner, testMap.Grid.Comp, new Vector2i(x, y), testMap.Tile.Tile);
                }
            }

            server.ResolveDependency<Robust.Server.Console.IServerConsoleHost>()
                .ExecuteCommand($"fixgridatmos {entMan.GetNetEntity(testMap.Grid.Owner)}");
        });

        await RunSeconds(1);

        await server.WaitPost(() =>
        {
            swarmer = entMan.SpawnAtPosition("MobSwarmer", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
            entMan.EventBus.RaiseLocalEvent(swarmer, new SwarmerEscapeActionEvent());
        });

        await RunSeconds(0.5f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.GetComponent<SwarmerComponent>(swarmer).Resources, Is.EqualTo(10), "Escape costs 10");
                Assert.That(entMan.GetComponent<TransformComponent>(swarmer).LocalPosition,
                    Is.Not.EqualTo(new System.Numerics.Vector2(0.5f, 0.5f)), "Swarmer should have jumped");
            });
        });
    }

    [Test]
    public async Task GlassShardsAreIntegrated()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        EntityUid swarmer = default;
        EntityUid shard = default;

        await server.WaitPost(() =>
        {
            swarmer = entMan.SpawnAtPosition("MobSwarmer", testMap.GridCoords);
            shard = entMan.SpawnAtPosition("ShardGlass", testMap.GridCoords);
            entMan.System<SharedInteractionSystem>().InteractHand(swarmer, shard);
        });

        // Несколько тиков, чтобы отработали отложенное удаление (QueueDel) и спавн лута/эффектов.
        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            // У осколков нет состава материалов, они разрешены отдельным списком.
            Assert.That(entMan.Deleted(shard), Is.True);
            Assert.That(entMan.GetComponent<SwarmerComponent>(swarmer).Resources, Is.EqualTo(21));
        });
    }

    [Test]
    public async Task IntegratedPackDropsItsContents()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        EntityUid pack = default;

        await server.WaitPost(() =>
        {
            var swarmer = entMan.SpawnAtPosition("MobSwarmer", testMap.GridCoords);
            pack = entMan.SpawnAtPosition("CigPackGreen", testMap.GridCoords);
            entMan.System<SharedInteractionSystem>().InteractHand(swarmer, pack);
        });

        // Несколько тиков, чтобы отработали отложенное удаление (QueueDel) и выпадение содержимого.
        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            var leftovers = entMan.System<EntityLookupSystem>()
                .GetEntitiesInRange(testMap.GridCoords, 2, LookupFlags.All | LookupFlags.Approximate)
                .Count(e => entMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID?.StartsWith("Cigarette") == true);

            // Пачка съедена, но сигареты из неё не пропали, а выпали на пол.
            Assert.That(entMan.Deleted(pack), Is.True);
            Assert.That(leftovers, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task AirlockLeavesSteel()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        EntityUid airlock = default;

        await server.WaitPost(() =>
        {
            // Шлюз можно снести только без разгерметизации: вокруг нужен пол с воздухом.
            var mapSys = entMan.System<SharedMapSystem>();
            for (var x = -1; x <= 2; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    mapSys.SetTile(testMap.Grid.Owner, testMap.Grid.Comp, new Vector2i(x, y), testMap.Tile.Tile);
                }
            }

            server.ResolveDependency<Robust.Server.Console.IServerConsoleHost>()
                .ExecuteCommand($"fixgridatmos {entMan.GetNetEntity(testMap.Grid.Owner)}");
        });

        await RunSeconds(1);

        await server.WaitPost(() =>
        {
            airlock = entMan.SpawnAtPosition("Airlock", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
            var swarmer = entMan.SpawnAtPosition("MobSwarmer", new EntityCoordinates(testMap.Grid, 1.5f, 0.5f));
            entMan.System<SharedInteractionSystem>().InteractHand(swarmer, airlock);
        });

        await RunSeconds(2);

        await server.WaitAssertion(() =>
        {
            var steel = 0;
            foreach (var uid in entMan.System<EntityLookupSystem>().GetEntitiesInRange(testMap.GridCoords, 2, LookupFlags.All | LookupFlags.Approximate))
            {
                if (entMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == "SheetSteel1")
                    steel += entMan.GetComponent<StackComponent>(uid).Count;
            }

            Assert.That(entMan.Deleted(airlock), Is.True);
            Assert.That(steel, Is.GreaterThanOrEqualTo(5));
        });
    }

    [TestCase("NuclearBomb")]
    [TestCase("NuclearBombKeg")]
    [TestCase("GravityGenerator")]
    [TestCase("GravityGeneratorMini")]
    [TestCase("APCBasic")]
    // На тестовой карте нет атмосферы: любая стена граничит с космосом, и её снос — разгерметизация.
    [TestCase("WallSolid")]
    public async Task RefusesForbiddenTargets(string proto)
    {
        var (target, _) = await ClickAndWait(proto, 11);
        Assert.That(Pair.Server.ResolveDependency<IEntityManager>().Deleted(target), Is.False);
    }

    [Test]
    public async Task RefusesLockerWithOrganismInside()
    {
        var (target, _) = await ClickAndWait("LockerSteel", 2, inside: "MobMouse");
        Assert.That(Pair.Server.ResolveDependency<IEntityManager>().Deleted(target), Is.False);
    }
}
