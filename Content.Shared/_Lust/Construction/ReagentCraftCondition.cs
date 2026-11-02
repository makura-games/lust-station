using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Construction;
using Content.Shared.Construction.Conditions;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Construction;

/// <summary>
/// Крафт доступен, только если в руках есть один набор и ёмкость, в которой только нужный реагент в нужном количестве.
/// </summary>
[DataDefinition]
public sealed partial class ReagentCraftCondition : IConstructionCondition
{
    [DataField(required: true)]
    public ProtoId<StackPrototype> Stack;

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true)]
    public FixedPoint2 Amount;

    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var hasStack = entMan.System<SharedHandsSystem>().EnumerateHeld(user)
            .Any(item => entMan.TryGetComponent(item, out StackComponent? stack) && stack.StackTypeId == Stack);

        if (hasStack && TryFindContainer(user, Reagent, Amount, out _))
            return true;

        entMan.System<SharedPopupSystem>().PopupEntity(Loc.GetString("construction-system-construct-no-materials"), user, user);
        return false;
    }

    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = "reagent-craft-condition",
            Arguments = new (string, object)[]
            {
                ("amount", Amount.Int()),
                ("reagent", IoCManager.Resolve<IPrototypeManager>().Index(Reagent).LocalizedName),
            },
        };
    }

    /// <summary>
    /// Ищет в руках ёмкость, в которой только нужный реагент в нужном количестве.
    /// </summary>
    public static bool TryFindContainer(EntityUid user, ProtoId<ReagentPrototype> reagent, FixedPoint2 amount, out Entity<SolutionComponent> solution)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var solutions = entMan.System<SharedSolutionContainerSystem>();
        foreach (var item in entMan.System<SharedHandsSystem>().EnumerateHeld(user))
        {
            if (!entMan.HasComponent<DrainableSolutionComponent>(item)
                || !solutions.TryGetDrainableSolution(item, out var soln, out var contents))
                continue;

            var quantity = contents.GetTotalPrototypeQuantity(reagent);
            if (quantity < amount || contents.Volume > quantity)
                continue;

            solution = soln.Value;
            return true;
        }

        solution = default;
        return false;
    }
}
