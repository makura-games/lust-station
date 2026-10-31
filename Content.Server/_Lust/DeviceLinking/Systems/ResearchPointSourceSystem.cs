using Content.Server.Research.Components;
using Content.Server.Research.Systems;
using Content.Shared.Research.Events;
using Content.Shared.Research.Components;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchPointsSourceSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddResearchPointsEvent>(OnAddPoints);
    }

    private void OnAddPoints(ref AddResearchPointsEvent args)
    {
        var server = FindResearchServer(args.Source);
        if (server == null)
            return;

        _research.ModifyServerPoints(server.Value, args.Amount);
    }

    private EntityUid? FindResearchServer(EntityUid source)
    {
        var query = EntityQueryEnumerator<ResearchServerComponent, TransformComponent>();
        var sourceXform = Transform(source);
        EntityUid? closest = null;
        var closestDistance = float.PositiveInfinity;

        while (query.MoveNext(out var serverUid, out _, out var serverXform))
        {
            if (!sourceXform.Coordinates.TryDistance(EntityManager, serverXform.Coordinates, out var distance))
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