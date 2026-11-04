using Content.Shared._Sunrise.VentCraw.Components;
using Content.Shared._Lust.Swarmer;
using Content.Shared._Lust.Swarmer.Components;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Server.Station.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._Lust.Swarmer;

public sealed partial class SwarmerSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StationSystem _station = default!;

    /// <summary>
    /// How many random tiles to try when looking for a safe spot to drop the victim.
    /// Stations are mostly empty space inside their bounding box, so this needs to be generous.
    /// </summary>
    private const int TeleportAttempts = 200;

    private void InitializeTeleport()
    {
        SubscribeLocalEvent<SwarmerComponent, SwarmerTeleportActionEvent>(OnTeleportAction);
        SubscribeLocalEvent<SwarmerComponent, SwarmerTeleportDoAfterEvent>(OnTeleportDoAfter);
        SubscribeLocalEvent<SwarmerComponent, SwarmerEscapeActionEvent>(OnEscapeAction);
    }

    private void OnTeleportAction(Entity<SwarmerComponent> ent, ref SwarmerTeleportActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartTeleport(ent, args.Target);
    }

    private void OnTeleportDoAfter(Entity<SwarmerComponent> ent, ref SwarmerTeleportDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = TryTeleport(ent, target);
    }

    private void OnEscapeAction(Entity<SwarmerComponent> ent, ref SwarmerEscapeActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryEscape(ent);
    }

    /// <summary>
    /// Grabs a living being and starts charging a teleport on it.
    /// </summary>
    public bool TryStartTeleport(Entity<SwarmerComponent> ent, EntityUid target)
    {
        if (!CanTeleport(ent, target))
            return false;

        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.TeleportDelay, new SwarmerTeleportDoAfterEvent(), ent, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        if (!DoAfter.TryStartDoAfter(doAfter))
            return false;

        Popup.PopupEntity(Loc.GetString("swarmer-teleport-start", ("swarmer", ent.Owner)), target, target, PopupType.LargeCaution);
        return true;
    }

    /// <summary>
    /// Restrains the victim with energy cuffs and drops it on a random safe tile of the station.
    /// </summary>
    public bool TryTeleport(Entity<SwarmerComponent> ent, EntityUid target)
    {
        if (!CanTeleport(ent, target))
            return false;

        if (!TryFindSafeTile(target, out var destination))
        {
            PopupSelf(ent, Loc.GetString("swarmer-teleport-failed"));
            return false;
        }

        RestrainVictim(ent, target);
        MoveWithEffects(ent, target, destination);
        return true;
    }

    /// <summary>
    /// Only living beings other than swarmers can be dispersed.
    /// </summary>
    public bool CanTeleport(Entity<SwarmerComponent> ent, EntityUid target, bool quiet = false)
    {
        if (HasComp<SwarmerComponent>(target) || !HasComp<MobStateComponent>(target))
        {
            if (!quiet)
                PopupSelf(ent, Loc.GetString("swarmer-teleport-invalid"));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Emergency jump: the swarmer itself blinks to a random safe spot on the station.
    /// </summary>
    public bool TryEscape(Entity<SwarmerComponent> ent)
    {
        if (!CanEscape(ent))
            return false;

        if (!TryFindSafeTile(ent, out var destination))
        {
            PopupSelf(ent, Loc.GetString("swarmer-teleport-failed"));
            return false;
        }

        ent.Comp.Resources -= ent.Comp.EscapeCost;
        Dirty(ent);

        MoveWithEffects(ent, ent, destination);
        return true;
    }

    /// <summary>
    /// Whether the swarmer can pay for the jump and is not inside the vents.
    /// </summary>
    public bool CanEscape(Entity<SwarmerComponent> ent, bool quiet = false)
    {
        // Из труб прыгать нельзя: там позицией управляет система вентиляции.
        if (HasComp<BeingVentCrawComponent>(ent))
            return false;

        return HasResources(ent, ent.Comp.EscapeCost, quiet);
    }

    private void RestrainVictim(Entity<SwarmerComponent> ent, EntityUid target)
    {
        if (ent.Comp.TeleportCuffs is not { } cuffsProto
            || !TryComp<CuffableComponent>(target, out var cuffable)
            || _cuffable.IsCuffed((target, cuffable)))
            return;

        // У прототипа наручников должно быть `used: true`: обычно этот флаг ставит do-after надевания,
        // а без него наручники невозможно снять.
        var cuffs = SpawnAtPosition(cuffsProto, Transform(target).Coordinates);
        if (!_cuffable.TryAddNewCuffs(target, ent, cuffs, cuffable))
            QueueDel(cuffs);
    }

    /// <summary>
    /// Moves an entity with sparks and teleport sounds on both ends.
    /// </summary>
    private void MoveWithEffects(Entity<SwarmerComponent> ent, EntityUid moved, EntityCoordinates destination)
    {
        var origin = Transform(moved).Coordinates;
        SpawnAtPosition(ent.Comp.TeleportEffect, origin);
        _audio.PlayPvs(ent.Comp.TeleportDepartureSound, origin);

        TransformSystem.SetCoordinates(moved, destination);
        TransformSystem.AttachToGridOrMap(moved);

        SpawnAtPosition(ent.Comp.TeleportEffect, destination);
        _audio.PlayPvs(ent.Comp.TeleportArrivalSound, moved);
    }

    /// <summary>
    /// Picks a random floor tile on the station's main grid (or the target's own grid if it's not on a station)
    /// that is not blocked and has breathable air.
    /// </summary>
    private bool TryFindSafeTile(EntityUid target, out EntityCoordinates destination)
    {
        destination = EntityCoordinates.Invalid;

        var gridUid = Transform(target).GridUid;
        if (_station.GetOwningStation(target) is { } station && _station.GetLargestGrid(station) is { } largest)
            gridUid = largest;

        if (gridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        if (Transform(grid).MapUid is not { } map)
            return false;

        var bounds = gridComp.LocalAABB;

        for (var i = 0; i < TeleportAttempts; i++)
        {
            var tile = new Vector2i(
                _random.Next((int) MathF.Floor(bounds.Left), (int) MathF.Ceiling(bounds.Right)),
                _random.Next((int) MathF.Floor(bounds.Bottom), (int) MathF.Ceiling(bounds.Top)));

            if (!Map.TryGetTileRef(grid, gridComp, tile, out var tileRef)
                || Turf.IsSpace(tileRef)
                || Turf.IsTileBlocked(tileRef, CollisionGroup.MobMask)
                || !_atmos.IsTileMixtureProbablySafe(grid, map, tile))
            {
                continue;
            }

            destination = Map.GridTileToLocal(grid, gridComp, tile);
            return true;
        }

        return false;
    }
}
