namespace Content.Shared._Lust.Smell;

/// <summary>
/// Raised on the held cleaner when the "wash scents" verb is executed. Handled on the server.
/// </summary>
public sealed class ScentCleaningExecuteEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Target;

    public ScentCleaningExecuteEvent(EntityUid user, EntityUid target)
    {
        User = user;
        Target = target;
    }
}
