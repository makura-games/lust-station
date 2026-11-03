using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class SexCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; } = false;

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField]
    public List<Sex> RequiredSex { get; private set; } = new();

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        if (CheckInitiator)
        {
            if (!entityManager.TryGetComponent<HumanoidProfileComponent>(initiator, out var initiatorAppearance))
                return false;

            if (!RequiredSex.Contains(initiatorAppearance.Sex))
                return false;
        }

        if (CheckTarget)
        {
            if (!entityManager.TryGetComponent<HumanoidProfileComponent>(target, out var targetAppearance))
                return false;

            if (!RequiredSex.Contains(targetAppearance.Sex))
                return false;
        }

        return true;
    }
}
