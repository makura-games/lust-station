using Content.Shared._Lust.Borgs;
using Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;
using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class LustGenitalCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; }

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField]
    public GenitalSlot RequiredGenital { get; private set; } = GenitalSlot.Penis;

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        if (CheckInitiator && !HasRequiredGenital(initiator, entityManager))
            return false;

        if (CheckTarget && !HasRequiredGenital(target, entityManager))
            return false;

        return true;
    }

    private bool HasRequiredGenital(EntityUid entity, EntityManager entityManager)
    {
        if (!BorgSexSystem.TryGetSex(entity, entityManager, out var sex))
            return false;

        return GenitalsHelper.GetGenitals(sex).Contains(RequiredGenital);
    }
}
