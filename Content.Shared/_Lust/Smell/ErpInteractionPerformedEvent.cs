namespace Content.Shared._Lust.Smell;

/// <summary>
/// Broadcast-ивент, поднимаемый ERP-системой, когда игрок выполнил
/// интимное взаимодействие. SmellSystem слушает его, чтобы добавить
/// временные запахи возбуждения/оргазма.
/// </summary>
public sealed class ErpInteractionPerformedEvent : EntityEventArgs
{
    public EntityUid User;
    public EntityUid Target;

    /// <summary>
    /// Строка-ключ для поиска ScentEventPrototype (например "arousal", "orgasm").
    /// </summary>
    public string? Trigger;
}
