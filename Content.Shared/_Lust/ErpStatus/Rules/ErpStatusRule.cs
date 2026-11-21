using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.ErpStatus;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ErpStatusRule
{
    /// <summary>
    ///     Инверсия.
    /// </summary>
    [DataField]
    public bool Inverted;

    /// <summary>
    ///     Проверяет попадает ли сущность под это правило.
    /// </summary>
    public bool Matches(EntityUid uid, IEntityManager entMan)
    {
        return Inverted != MatchesCore(uid, entMan);
    }

    protected abstract bool MatchesCore(EntityUid uid, IEntityManager entMan);

    /// <summary>
    ///     Пытается получить должность куклы через её разум(если он есть).
    /// </summary>
    /// <returns><c>true</c> если у сущности есть разум и назначенная должность</returns>
    protected static bool TryGetJob(EntityUid uid, IEntityManager entMan, out ProtoId<JobPrototype>? job)
    {
        job = null;

        if (!entMan.System<SharedMindSystem>().TryGetMind(uid, out var mindId, out _))
            return false;

        return entMan.System<SharedJobSystem>().MindTryGetJobId(mindId, out job) && job != null;
    }
}
