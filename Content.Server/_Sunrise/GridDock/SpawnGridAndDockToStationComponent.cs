using Robust.Shared.Utility;
using Robust.Shared.Random; // Lust-add

namespace Content.Server._Sunrise.GridDock;

[RegisterComponent]
public sealed partial class SpawnGridAndDockToStationComponent : Component
{
    [DataField(required: true)]
    public List<GridDockEntry> Grids { get; set; } = new();
}

[DataDefinition]
public sealed partial class GridDockEntry
{
	// Lust-start
    [DataField]
    public ResPath? GridPath;
	
    [DataField]
    public List<ResPath> GridPaths = new();
	// Lust-end

    [DataField(required: true)]
    public string PriorityTag;
	
	// Lust-start
	public ResPath PickPath(IRobustRandom random)
    {
        if (GridPaths.Count > 0)
            return random.Pick(GridPaths);

        return GridPath ?? ResPath.Empty;
    }
	// Lust-end
}
