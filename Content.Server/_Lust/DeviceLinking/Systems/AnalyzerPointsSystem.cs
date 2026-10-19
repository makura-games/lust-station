using Content.Server.Research.Components;
using Content.Server.Research.Systems;
using Content.Shared.Research.Components;
using Content.Shared.Analyzer;

namespace Content.Server.Analyzer;

public sealed partial class AnalyzerPointsSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnalyzerComponent, AnalyzerAddPointsEvent>(OnAddPoints);
    }

    private void OnAddPoints(Entity<AnalyzerComponent> ent, ref AnalyzerAddPointsEvent args)
    {
        var server = FindResearchServer(ent);
        if (server == null)
            return;

        _research.ModifyServerPoints(server.Value, args.Amount);
    }

    private EntityUid? FindResearchServer(EntityUid analyzer)
    {
        var query = EntityQueryEnumerator<ResearchServerComponent, TransformComponent>();
        var analyzerXform = Transform(analyzer);
        EntityUid? closest = null;
        var closestDistance = float.PositiveInfinity;

        while (query.MoveNext(out var serverUid, out _, out var serverXform))
        {
            if (!analyzerXform.Coordinates.TryDistance(EntityManager, serverXform.Coordinates, out var distance))
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