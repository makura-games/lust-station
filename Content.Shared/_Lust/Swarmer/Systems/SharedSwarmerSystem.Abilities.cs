using Content.Shared._Lust.Swarmer.Components;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Swarmer.Systems;

public abstract partial class SharedSwarmerSystem
{
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private INetManager _net = default!;

    /// <summary>
    /// Search radius around a build spot when checking whether a swarmer structure already stands there.
    /// </summary>
    private const float StructureLookupRange = 0.4f;

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<SwarmerComponent, SwarmerBarricadeActionEvent>(OnBarricadeAction);
        SubscribeLocalEvent<SwarmerComponent, SwarmerBarricadeDoAfterEvent>(OnBarricadeDoAfter);
        SubscribeLocalEvent<SwarmerComponent, SwarmerTrapActionEvent>(OnTrapAction);
        SubscribeLocalEvent<SwarmerComponent, SwarmerSelfRepairActionEvent>(OnSelfRepairAction);
        SubscribeLocalEvent<SwarmerComponent, SwarmerSelfRepairDoAfterEvent>(OnSelfRepairDoAfter);
        SubscribeLocalEvent<SwarmerComponent, SwarmerToggleLightActionEvent>(OnToggleLightAction);

        SubscribeLocalEvent<SwarmerStructureComponent, PreventCollideEvent>(OnStructurePreventCollide);
        SubscribeLocalEvent<SwarmerTrapComponent, StepTriggerAttemptEvent>(OnTrapStepAttempt);
    }

    private void OnBarricadeAction(Entity<SwarmerComponent> ent, ref SwarmerBarricadeActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartBuild(ent, ent.Comp.BarricadeCost, ent.Comp.BarricadeDelay, new SwarmerBarricadeDoAfterEvent());
    }

    private void OnBarricadeDoAfter(Entity<SwarmerComponent> ent, ref SwarmerBarricadeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = TryBuild(ent, TransformSystem.GetMoverCoordinates(ent), ent.Comp.BarricadePrototype, ent.Comp.BarricadeCost, snapToGrid: true);
    }

    private void OnTrapAction(Entity<SwarmerComponent> ent, ref SwarmerTrapActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryBuild(ent, args.Target, ent.Comp.TrapPrototype, ent.Comp.TrapCost, snapToGrid: false);
    }

    private void OnSelfRepairAction(Entity<SwarmerComponent> ent, ref SwarmerSelfRepairActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartSelfRepair(ent);
    }

    private void OnSelfRepairDoAfter(Entity<SwarmerComponent> ent, ref SwarmerSelfRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        _damageable.ClearAllDamage(ent.Owner);
        Popup.PopupClient(Loc.GetString("swarmer-self-repair-done"), ent, ent);
    }

    private void OnToggleLightAction(Entity<SwarmerComponent> ent, ref SwarmerToggleLightActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryToggleLight(ent);
    }

    /// <summary>
    /// Swarmers and their own shots pass through swarmer barricades.
    /// </summary>
    private void OnStructurePreventCollide(Entity<SwarmerStructureComponent> ent, ref PreventCollideEvent args)
    {
        if (!ent.Comp.SwarmerPassable)
            return;

        var other = args.OtherEntity;
        if (HasComp<SwarmerComponent>(other)
            || TryComp<ProjectileComponent>(other, out var projectile) && HasComp<SwarmerComponent>(projectile.Shooter))
        {
            args.Cancelled = true;
        }
    }

    /// <summary>
    /// Step triggers only fire if someone allows it: the trap goes off for any living being except swarmers.
    /// </summary>
    private void OnTrapStepAttempt(Entity<SwarmerTrapComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (HasComp<SwarmerComponent>(args.Tripper) || !HasComp<MobStateComponent>(args.Tripper))
        {
            args.Cancelled = true;
            return;
        }

        args.Continue = true;
    }

    /// <summary>
    /// Starts the do-after for building a grid-aligned structure on the swarmer's own tile.
    /// </summary>
    public bool TryStartBuild(Entity<SwarmerComponent> ent, int cost, TimeSpan delay, SimpleDoAfterEvent doAfterEvent)
    {
        if (!CanBuild(ent, TransformSystem.GetMoverCoordinates(ent), cost, snapToGrid: true))
            return false;

        var doAfter = new DoAfterArgs(EntityManager, ent, delay, doAfterEvent, ent)
        {
            BreakOnMove = true,
        };

        return DoAfter.TryStartDoAfter(doAfter);
    }

    /// <summary>
    /// Pays for and builds a structure at the given coordinates.
    /// </summary>
    /// <param name="snapToGrid">Put the structure in the center of the tile instead of exactly at the coordinates.</param>
    public bool TryBuild(Entity<SwarmerComponent> ent, EntityCoordinates coordinates, EntProtoId prototype, int cost, bool snapToGrid)
    {
        // Пока шла постройка, место могли занять.
        if (!CanBuild(ent, coordinates, cost, snapToGrid))
            return false;

        ent.Comp.Resources -= cost;
        Dirty(ent);

        // Постройки сетевые, поэтому их спавнит только сервер.
        if (_net.IsServer && TryGetPlacement(coordinates, snapToGrid, out var placement))
        {
            var structure = SpawnAtPosition(prototype, placement);
            // Постройка не наследует поворот того, кто её ставил.
            TransformSystem.SetLocalRotation(structure, Angle.Zero);
        }

        return true;
    }

    /// <summary>
    /// Checks that the swarmer can pay for a structure and that the target spot is reachable, solid and free.
    /// </summary>
    public bool CanBuild(Entity<SwarmerComponent> ent, EntityCoordinates coordinates, int cost, bool snapToGrid, bool quiet = false)
    {
        if (!CanAfford(ent, cost, quiet))
            return false;

        if (!coordinates.IsValid(EntityManager))
            return false;

        if (!_interaction.InRangeUnobstructed(ent.Owner, coordinates, popup: !quiet))
            return false;

        if (!TryGetTileCenter(coordinates, out var center)
            || !TryGetTileRef(center, out var tileRef)
            || Turf.IsSpace(tileRef)
            || Turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
        {
            if (!quiet)
                Popup.PopupClient(Loc.GetString("swarmer-build-invalid-tile"), ent, ent);
            return false;
        }

        var placement = snapToGrid ? center : coordinates;
        if (_lookup.GetEntitiesInRange<SwarmerStructureComponent>(placement, StructureLookupRange).Count > 0)
        {
            if (!quiet)
                Popup.PopupClient(Loc.GetString("swarmer-build-occupied"), ent, ent);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Starts repairing all damage while the swarmer holds still.
    /// </summary>
    public bool TryStartSelfRepair(Entity<SwarmerComponent> ent)
    {
        if (!CanSelfRepair(ent))
            return false;

        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.SelfRepairDelay, new SwarmerSelfRepairDoAfterEvent(), ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        if (!DoAfter.TryStartDoAfter(doAfter))
            return false;

        Popup.PopupClient(Loc.GetString("swarmer-self-repair-start"), ent, ent);
        return true;
    }

    /// <summary>
    /// Whether the swarmer has any damage to repair.
    /// </summary>
    public bool CanSelfRepair(Entity<SwarmerComponent> ent, bool quiet = false)
    {
        if (TryComp<DamageableComponent>(ent, out var damageable)
            && !_damageable.TryGetDamageGreaterThan((ent, damageable), FixedPoint2.Zero, out _))
        {
            if (!quiet)
                Popup.PopupClient(Loc.GetString("swarmer-self-repair-intact"), ent, ent);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Switches the swarmer's glow on or off.
    /// </summary>
    public bool TryToggleLight(Entity<SwarmerComponent> ent)
    {
        if (!PointLight.TryGetLight(ent, out var light))
            return false;

        PointLight.SetEnabled(ent, !light.Enabled, light);
        return true;
    }

    /// <summary>
    /// Whether the swarmer has enough resources, telling it how many it needs if not.
    /// </summary>
    public bool CanAfford(Entity<SwarmerComponent> ent, int cost, bool quiet = false)
    {
        if (ent.Comp.Resources >= cost)
            return true;

        if (!quiet)
            Popup.PopupClient(Loc.GetString("swarmer-build-no-resources", ("cost", cost)), ent, ent);
        return false;
    }

    /// <summary>
    /// The center of the grid tile containing the coordinates, so structures line up with the grid.
    /// </summary>
    private bool TryGetTileCenter(EntityCoordinates coordinates, out EntityCoordinates center)
    {
        center = coordinates;
        if (TransformSystem.GetGrid(coordinates) is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var tile = Map.TileIndicesFor(grid, gridComp, coordinates);
        center = Map.GridTileToLocal(grid, gridComp, tile);
        return true;
    }

    /// <summary>
    /// Where a structure goes: the center of the tile, or exactly the given spot.
    /// </summary>
    private bool TryGetPlacement(EntityCoordinates coordinates, bool snapToGrid, out EntityCoordinates placement)
    {
        if (snapToGrid)
            return TryGetTileCenter(coordinates, out placement);

        // Клик может прийти в координатах карты, а постройка должна держаться за станцию.
        placement = TransformSystem.GetMoverCoordinates(coordinates);
        return true;
    }

    private bool TryGetTileRef(EntityCoordinates coordinates, out TileRef tileRef)
    {
        tileRef = default;
        return TransformSystem.GetGrid(coordinates) is { } grid
               && TryComp<MapGridComponent>(grid, out var gridComp)
               && Map.TryGetTileRef(grid, gridComp, coordinates, out tileRef);
    }
}
