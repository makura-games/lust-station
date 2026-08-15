using Robust.Shared.GameObjects;

namespace Content.Shared._Lust.Smell;

/// <summary>
/// Broadcast-ивент жизненной системы панели взаимодействий: существо перешагнуло порог
/// возбуждения и теперь «источает» его. Рейзится ERP-системой; подписчики (SmellSystem)
/// навешивают соответствующие временные эффекты на носителя.
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
