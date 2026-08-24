using Robust.Shared.GameObjects;

namespace Content.Shared._Lust.Smell;

/// <summary>
/// Broadcast-ивент жизненной системы панели взаимодействий: сущность находится
/// в состоянии возбуждения (любовь ≥ 33%). Рейзится ERP-системой при каждом
/// действии, поддерживающем возбуждение; подписчики освежают временный запах
/// возбуждения носителя.
/// </summary>
public sealed class ArousalStartedEvent : EntityEventArgs
{
    public EntityUid Uid;
}

/// <summary>
/// Broadcast-ивент жизненной системы: существо испытало оргазм. User — кто кончил,
/// Target — получатель эффекта (партнёр либо сам User). Рейзится ERP-системой.
/// </summary>
public sealed class OrgasmPerformedEvent : EntityEventArgs
{
    public EntityUid User;
    public EntityUid Target;
}
