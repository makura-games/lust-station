using Content.Shared._Lust.Borgs;
using Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;
using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class LustSexCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; }

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField]
    public List<Sex> RequiredSex { get; private set; } = new();

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        if (CheckInitiator &&
            (!BorgSexSystem.TryGetSex(initiator, entityManager, out var initiatorSex) || !RequiredSex.Contains(initiatorSex)))
            return false;

        if (CheckTarget &&
            (!BorgSexSystem.TryGetSex(target, entityManager, out var targetSex) || !RequiredSex.Contains(targetSex)))
            return false;

        return true;
    }
}
