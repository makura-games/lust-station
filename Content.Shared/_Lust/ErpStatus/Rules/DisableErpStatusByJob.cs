using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.ErpStatus;

/// <summary>
///     Запрещает ERP перечисленным должностям на время действия кода тревоги
/// </summary>
public sealed partial class DisableErpStatusByJob : ErpStatusRule
{
    /// <summary>
    ///     Должности которым запрещено шпиливили.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<JobPrototype>> Jobs = [];

    protected override bool MatchesCore(EntityUid uid, IEntityManager entMan)
    {
        return TryGetJob(uid, entMan, out var job) && Jobs.Contains(job!.Value);
    }
}
