using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class GenitalCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; }

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField]
    public GenitalSlot RequiredGenital { get; private set; } = GenitalSlot.Penis;

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        if (CheckInitiator)
        {
            if (!HasRequiredGenital(initiator, entityManager))
                return false;
        }

        if (CheckTarget)
        {
            if (!HasRequiredGenital(target, entityManager))
                return false;
        }

        return true;
    }

    private bool HasRequiredGenital(EntityUid entity, EntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<HumanoidProfileComponent>(entity, out var appearance))
            return false;

        var genitals = GenitalsHelper.GetGenitals(appearance.Sex);
        return genitals.Contains(RequiredGenital);
    }
}
