using System.Linq;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.ErpStatus;

/// <summary>
///     Запрещает ERP всем должностям перечисленных отделов пока действует код
/// </summary>
public sealed partial class DisableErpStatusByDepartment : ErpStatusRule
{
    /// <summary>
    ///     Отделы которым запрещено чпокатся
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<DepartmentPrototype>> Departments = [];

    protected override bool MatchesCore(EntityUid uid, IEntityManager entMan)
    {
        if (!TryGetJob(uid, entMan, out var job))
            return false;

        var jobs = entMan.System<SharedJobSystem>();
        return Departments.Any(department
            => jobs.JobIsInDepartment(job!.Value, department));
    }
}
