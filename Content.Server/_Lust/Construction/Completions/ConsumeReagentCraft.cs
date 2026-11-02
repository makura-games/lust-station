using Content.Server.Stack;
using Content.Shared._Lust.Construction;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Construction;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.Construction.Completions;

/// <summary>
/// Списывает реагент из ёмкости в руках. Если состав убрали во время крафта, готовый предмет не выдаётся, а исходный возвращается.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class ConsumeReagentCraft : IGraphAction
{
    /// <summary>
    /// Стак, одна единица которого уже потрачена на крафт.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<StackPrototype> Stack;

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true)]
    public FixedPoint2 Amount;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (userUid is not { } user)
            return;

        if (ReagentCraftCondition.TryFindContainer(user, Reagent, Amount, out var solution))
        {
            // В ёмкости только этот реагент, поэтому забираем порцию целиком, независимо от данных реагента
            entityManager.System<SharedSolutionContainerSystem>().SplitSolution(solution, Amount);
            return;
        }

        var stacks = entityManager.System<StackSystem>();
        var refund = stacks.SpawnAtPosition(1, Stack, entityManager.GetComponent<TransformComponent>(user).Coordinates);
        stacks.TryMergeToHands(refund, user);

        entityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("construction-system-construct-no-materials"), user, user);
        // Без стака результат не сольётся с готовой стопкой в руках до отложенного удаления
        entityManager.RemoveComponent<StackComponent>(uid);
        entityManager.QueueDeleteEntity(uid);
    }
}
