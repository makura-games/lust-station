namespace Content.Shared.Analyzer;

[ByRefEvent]
public record struct AnalyzerAddPointsEvent(EntityUid Analyzer, int Amount);