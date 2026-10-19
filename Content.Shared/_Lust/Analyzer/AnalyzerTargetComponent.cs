using Robust.Shared.GameStates;

namespace Content.Shared.Analyzer;

/// <summary>
/// маркер для вещей что можно сканить
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AnalyzerTargetComponent : Component
{
}