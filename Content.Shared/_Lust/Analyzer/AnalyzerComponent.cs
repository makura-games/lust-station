using Robust.Shared.GameStates;

namespace Content.Shared.Analyzer;

/// <summary>
/// Analyzer that gives a random target component to find.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnalyzerComponent : Component
{
    [DataField]
    public List<string> PossibleComponents = new();

    [AutoNetworkedField]
    public string? CurrentTargetComponent;

    [DataField]
    public int PointsPerScan = 7500;
}