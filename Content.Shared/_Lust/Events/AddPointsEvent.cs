namespace Content.Shared.ProximityDetection.Events;

[ByRefEvent]
public record struct AddResearchPointsEvent(EntityUid Detector, int Amount);