namespace Content.Shared._Lust.Smell;

/// <summary>
/// Raised directed at a scent bearer (<see cref="Components.ScentComponent"/>) when a smeller
/// executes the "smell" verb.
/// </summary>
public sealed class SmellExecuteEvent : EntityEventArgs
{
    public readonly EntityUid User;

    public SmellExecuteEvent(EntityUid user)
    {
        User = user;
    }
}
