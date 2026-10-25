using System.Linq;
using Content.Server._Sunrise.Doors.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._Sunrise.Doors.Systems;

public sealed partial class SunriseMultiTileAirtightSystem
{
    [Dependency] private SharedMapSystem _map = null!;

    [Dependency] private EntityQuery<SunriseMultiTileAirtightBlockerComponent> _blockerQuery;
    [Dependency] private EntityQuery<SunriseMultiTileAirtightComponent> _multiTileQuery;

    private readonly List<EntityUid> _anchoredEntities = [];

    private readonly HashSet<EntityUid> _pendingUnpaused = [];
    private readonly HashSet<EntityUid> _pausedGrids = [];

    private int _frameCounter;

    private void OnUnpaused(Entity<SunriseMultiTileAirtightComponent> ent, ref EntityUnpausedEvent args)
    {
        _pendingUnpaused.Add(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pausedGrids.Count > 0 && _frameCounter++ % 10 == 0)
            _pausedGrids.Clear();

        if (_pendingUnpaused.Count == 0)
            return;

        var entitiesToProcess = _pendingUnpaused.ToArray();
        _pendingUnpaused.Clear();

        _pausedGrids.Clear();

        foreach (var uid in entitiesToProcess)
        {
            RefreshAfterUnpause(uid);
        }
    }

    private void RefreshAfterUnpause(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return;

        if (!_multiTileQuery.TryGetComponent(uid, out var component))
            return;

        var ent = (uid, component);
        RefreshGeometry(ent);
        RefreshAirblock(ent);
    }

    private bool IsMapOrGridPaused(EntityUid gridUid)
    {
        if (Paused(gridUid))
            return true;

        return _xformQuery.TryGetComponent(gridUid, out var gridXform)
               && gridXform.MapUid is { } mapUid
               && Paused(mapUid);
    }

    private void DeleteStaleBlockersOnTile(Entity<MapGridComponent> gridEnt, Vector2i tile, EntityUid sourceDoor)
    {
        _anchoredEntities.Clear();
        _map.GetAnchoredEntities(gridEnt, tile, _anchoredEntities);

        foreach (var blocker
                 in from blocker
                     in _anchoredEntities
                 where _blockerQuery.HasComp(blocker)
                 where !TerminatingOrDeleted(blocker)
                 where !IsManagedByOtherDoor(blocker, sourceDoor)
                 select blocker)
        {
            Del(blocker);
        }

        _anchoredEntities.Clear();
    }

    private bool IsManagedByOtherDoor(EntityUid blocker, EntityUid sourceDoor)
    {
        var query = EntityQueryEnumerator<SunriseMultiTileAirtightComponent>();
        while (query.MoveNext(out var door, out var multiTile))
        {
            if (door == sourceDoor || TerminatingOrDeleted(door))
                continue;

            if (multiTile.Blockers.Contains(blocker))
                return true;
        }

        return false;
    }
}
