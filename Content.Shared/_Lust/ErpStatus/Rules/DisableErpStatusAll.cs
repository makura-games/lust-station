using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.ErpStatus;

/// <summary>
///     Запрещает ERP всем на время действия кода тревоги.
///     Список <see cref="ExceptJobs"/> задаёт должности-исключения.
/// </summary>
public sealed partial class DisableErpStatusAll : ErpStatusRule
{
    /// <summary>
    ///     Должности на которые запрет не распространяется.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> ExceptJobs = [];

    protected override bool MatchesCore(EntityUid uid, IEntityManager entMan)
    {
        // дрочилы без должности всё равно попадает под запрет
        if (!TryGetJob(uid, entMan, out var job))
            return true;

        return !ExceptJobs.Contains(job!.Value);
    }
}
