namespace Content.Shared.Research.Events;

[ByRefEvent]
public record struct AddResearchPointsEvent(EntityUid Source, int Amount);