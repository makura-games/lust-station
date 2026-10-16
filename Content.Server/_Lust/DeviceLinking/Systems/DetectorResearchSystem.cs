using Content.Server.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Server.Research.Systems;
using Content.Shared.Research.Components;
using Content.Shared.ProximityDetection.Components;
using Content.Shared.ProximityDetection.Events;

namespace Content.Server.ProximityDetection;

public sealed partial class ProximityDetectionPointsSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProximityDetectorComponent, AddResearchPointsEvent>(OnAddResearchPoints);
    }

    private void OnAddResearchPoints(Entity<ProximityDetectorComponent> ent, ref AddResearchPointsEvent args)
    {
        var server = FindResearchServer(ent);
        if (server == null)
            return;

        _research.ModifyServerPoints(server.Value, args.Amount);
    }

    private EntityUid? FindResearchServer(EntityUid detector)
    {
        var query = EntityQueryEnumerator<ResearchServerComponent, TransformComponent>();
        var detectorXform = Transform(detector);
        EntityUid? closest = null;
        var closestDistance = float.PositiveInfinity;

        while (query.MoveNext(out var serverUid, out _, out var serverXform))
        {
            if (!detectorXform.Coordinates.TryDistance(EntityManager, serverXform.Coordinates, out var distance))
                continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = serverUid;
            }
        }

        return closest;
    }
}