using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction.Components;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Stack;
using Content.Shared._Lust.Swarmer;
using Content.Shared._Lust.Swarmer.Components;
using Content.Shared._Lust.Swarmer.Systems;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.Swarmer;

/// <summary>
/// Server side of swarmers: taking structures apart (needs atmospherics), teleports, replication and dormancy.
/// </summary>
public sealed partial class SwarmerSystem : SharedSwarmerSystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private DestructibleSystem _destructible = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private StackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwarmerComponent, SwarmerDisintegrateDoAfterEvent>(OnDisintegrateDoAfter);
        SubscribeLocalEvent<SwarmerComponent, SwarmerDismantleDoAfterEvent>(OnDismantleDoAfter);

        InitializeTeleport();
        InitializeGlow();
        InitializeReplicate();
        InitializeDormancy();
        InitializeSpeech();
    }

    private void OnDisintegrateDoAfter(Entity<SwarmerComponent> ent, ref SwarmerDisintegrateDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = TryDisintegrate(ent, target);
    }

    private void OnDismantleDoAfter(Entity<SwarmerComponent> ent, ref SwarmerDismantleDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = TryDismantle(ent, target);
    }

    /// <summary>
    /// Starts taking the target apart: a slow dismantle for machines, a quick disintegration for everything else.
    /// </summary>
    public override bool TryStartDisintegrate(Entity<SwarmerComponent> ent, EntityUid target)
    {
        if (!CanDisintegrate(ent, target, out var dismantle))
            return false;

        var delay = dismantle ? ent.Comp.DismantleDelay : ent.Comp.DisintegrateDelay;
        DoAfterEvent doAfterEvent = dismantle ? new SwarmerDismantleDoAfterEvent() : new SwarmerDisintegrateDoAfterEvent();

        var doAfter = new DoAfterArgs(EntityManager, ent, delay, doAfterEvent, ent, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        if (!DoAfter.TryStartDoAfter(doAfter))
            return false;

        if (dismantle)
            Spawn(ent.Comp.DismantleEffect, TransformSystem.GetMapCoordinates(target));

        return true;
    }

    /// <summary>
    /// Destroys a structure. Walls, girders, tables and seats break apart into their materials instead of vanishing.
    /// </summary>
    public bool TryDisintegrate(Entity<SwarmerComponent> ent, EntityUid target)
    {
        // Пока шла работа, обстановка могла измениться, например комнату разгерметизировали.
        if (!CanDisintegrate(ent, target, out _))
            return false;

        Spawn(ent.Comp.DisintegrateEffect, TransformSystem.GetMapCoordinates(target));
        DropContents(target);
        DropExtraLoot(ent, target);

        if (ShouldSalvage(ent, target))
            Salvage(ent, target);
        else
            QueueDel(target);

        return true;
    }

    /// <summary>
    /// Takes a machine apart: its board and parts drop, plus some steel.
    /// </summary>
    public bool TryDismantle(Entity<SwarmerComponent> ent, EntityUid target)
    {
        if (!CanDisintegrate(ent, target, out _))
            return false;

        var coords = TransformSystem.GetMapCoordinates(target);
        DropContents(target);

        var loot = Spawn(ent.Comp.DismantleLoot, coords);
        _stack.SetCount((loot, null), ent.Comp.DismantleLootAmount);

        QueueDel(target);
        return true;
    }

    /// <summary>
    /// Checks whether the swarmer is willing to take the target apart, telling it why not if it isn't.
    /// </summary>
    /// <param name="dismantle">True for machines, which are dismantled slowly and drop their parts.</param>
    public bool CanDisintegrate(Entity<SwarmerComponent> ent, EntityUid target, out bool dismantle, bool quiet = false)
    {
        dismantle = false;

        foreach (var forbidden in ent.Comp.ForbiddenTargets)
        {
            if (!Whitelist.IsWhitelistPass(forbidden.Whitelist, target))
                continue;

            if (!quiet)
                PopupSelf(ent, Loc.GetString(forbidden.Message, ("target", target)));
            return false;
        }

        if (ContainsOrganism(target))
        {
            if (!quiet)
                PopupSelf(ent, Loc.GetString("swarmer-disintegrate-organism"));
            return false;
        }

        dismantle = HasComp<MachineComponent>(target) || HasComp<ComputerComponent>(target);

        // То, что нельзя ни сломать, ни разобрать, свармеру тоже недоступно.
        if (!dismantle && !HasComp<DestructibleComponent>(target) && !HasComp<ConstructionComponent>(target))
        {
            if (!quiet)
                PopupSelf(ent, Loc.GetString("swarmer-disintegrate-incompatible", ("target", target)));
            return false;
        }

        if (HasComp<AirtightComponent>(target) && !IsSafeToRemove(ent, target, quiet))
            return false;

        return true;
    }

    /// <summary>
    /// Walls, windows and doors may only go if that won't open the station to space or release a lot of pressure.
    /// </summary>
    private bool IsSafeToRemove(Entity<SwarmerComponent> ent, EntityUid target, bool quiet)
    {
        var xform = Transform(target);
        if (xform.GridUid is not { } grid)
        {
            if (!quiet)
                PopupSelf(ent, Loc.GetString("swarmer-disintegrate-breach"));
            return false;
        }

        var center = TransformSystem.GetGridTilePositionOrDefault((target, xform));
        var minPressure = float.MaxValue;
        var maxPressure = float.MinValue;

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                var tile = center + new Vector2i(x, y);

                if (_atmos.IsTileSpace(grid, xform.MapUid, tile))
                {
                    if (!quiet)
                        PopupSelf(ent, Loc.GetString("swarmer-disintegrate-breach"));
                    return false;
                }

                if (_atmos.GetTileMixture(grid, xform.MapUid, tile) is not { } mixture)
                    continue;

                minPressure = MathF.Min(minPressure, mixture.Pressure);
                maxPressure = MathF.Max(maxPressure, mixture.Pressure);
            }
        }

        if (maxPressure - minPressure > ent.Comp.MaxPressureDelta)
        {
            if (!quiet)
                PopupSelf(ent, Loc.GetString("swarmer-disintegrate-pressure"));
            return false;
        }

        return true;
    }

    private bool ShouldSalvage(Entity<SwarmerComponent> ent, EntityUid target)
    {
        if (Whitelist.IsWhitelistPass(ent.Comp.SalvageWhitelist, target))
            return true;

        return TryComp<ConstructionComponent>(target, out var construction)
               && ent.Comp.SalvageGraphs.Contains(construction.Graph);
    }

    /// <summary>
    /// Breaks the target the way it breaks when destroyed by damage (its weakest damage threshold),
    /// but every possible drop is guaranteed to spawn exactly once instead of being random.
    /// </summary>
    private void Salvage(Entity<SwarmerComponent> ent, EntityUid target)
    {
        DamageThreshold? weakest = null;
        if (TryComp<DestructibleComponent>(target, out var destructible))
        {
            foreach (var threshold in destructible.Thresholds)
            {
                if (threshold.Trigger is not DamageTrigger trigger)
                    continue;

                if (weakest?.Trigger is not DamageTrigger weakestTrigger || trigger.Damage < weakestTrigger.Damage)
                    weakest = threshold;
            }
        }

        var coords = TransformSystem.GetMapCoordinates(target);
        var replaced = false;
        var dropped = false;

        foreach (var behavior in weakest?.Behaviors ?? [])
        {
            switch (behavior)
            {
                case SpawnEntitiesBehavior spawn:
                    foreach (var proto in spawn.Spawn.Keys)
                    {
                        // Прототип стака может по умолчанию быть полной пачкой, а нужна одна штука.
                        var loot = Spawn(proto, coords);
                        if (TryComp<StackComponent>(loot, out var stack))
                            _stack.SetCount((loot, stack), 1);
                        dropped = true;
                    }
                    break;
                // Превращает стену в балку на том же месте.
                case ChangeConstructionNodeBehavior:
                    replaced = true;
                    behavior.Execute(target, _destructible, ent);
                    break;
                // Цель удаляем сами.
                case DoActsBehavior:
                    break;
                default:
                    behavior.Execute(target, _destructible, ent);
                    break;
            }
        }

        if (replaced)
            return;

        // При поломке ничего не выпадает (большинство стульев), возвращаем материалы из рецепта постройки.
        if (!dropped)
            RefundConstructionMaterials(target);

        QueueDel(target);
    }

    /// <summary>
    /// Drops one piece of every material used to build the target, following its construction graph.
    /// </summary>
    private void RefundConstructionMaterials(EntityUid target)
    {
        if (!TryComp<ConstructionComponent>(target, out var construction)
            || !_proto.TryIndex<ConstructionGraphPrototype>(construction.Graph, out var graph)
            || graph.Start is not { } start
            || !graph.TryPath(start, construction.Node, out var path))
            return;

        var materials = new HashSet<ProtoId<StackPrototype>>();
        var previous = start;
        foreach (var node in path)
        {
            if (graph.Edge(previous, node.Name) is { } edge)
            {
                foreach (var step in edge.Steps)
                {
                    if (step is MaterialConstructionGraphStep material)
                        materials.Add(material.MaterialPrototypeId);
                }
            }

            previous = node.Name;
        }

        var coords = Transform(target).Coordinates;
        foreach (var material in materials)
        {
            _stack.SpawnAtPosition(1, material, coords);
        }
    }

    /// <summary>
    /// Spawns the extra loot configured for this kind of structure, e.g. steel from airlocks.
    /// </summary>
    private void DropExtraLoot(Entity<SwarmerComponent> ent, EntityUid target)
    {
        var coords = Transform(target).Coordinates;
        foreach (var loot in ent.Comp.ExtraLoot)
        {
            if (!Whitelist.IsWhitelistPass(loot.Whitelist, target))
                continue;

            var spawned = SpawnAtPosition(loot.Spawn, coords);
            if (TryComp<StackComponent>(spawned, out var stack))
                _stack.SetCount((spawned, stack), loot.Amount);
        }
    }

    private bool ContainsOrganism(EntityUid uid)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var manager))
            return false;

        foreach (var container in Container.GetAllContainers(uid, manager))
        {
            foreach (var contained in container.ContainedEntities)
            {
                if (HasComp<MobStateComponent>(contained) || ContainsOrganism(contained))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resource check for actions only the server handles (replication, the emergency jump).
    /// The shared check shows its popup through prediction, which never runs for these actions.
    /// </summary>
    private bool HasResources(Entity<SwarmerComponent> ent, int cost, bool quiet = false)
    {
        if (CanAfford(ent, cost, quiet: true))
            return true;

        if (!quiet)
            PopupSelf(ent, Loc.GetString("swarmer-build-no-resources", ("cost", cost)));
        return false;
    }

    /// <summary>
    /// Shows a warning only the swarmer itself can see.
    /// </summary>
    private void PopupSelf(EntityUid swarmer, string message)
    {
        Popup.PopupEntity(message, swarmer, swarmer, PopupType.SmallCaution);
    }
}
