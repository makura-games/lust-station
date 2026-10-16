using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Chemistry;

/// <summary>
/// Позволяет превратить предмет в другой, пропитав его реагентом из ёмкости.
/// </summary>
[RegisterComponent, Access(typeof(ReagentCraftSystem))]
public sealed partial class ReagentCraftComponent : Component
{

    [DataField(required: true)]
    public List<ReagentCraftRecipe> Recipes = new();
}

/// <summary>
/// Реагент = результат
/// </summary>
[DataDefinition]
public sealed partial class ReagentCraftRecipe
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent = string.Empty;

    /// <summary>
    /// Минимальное количество реагента для срабатывания.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MinQuantity;

    /// <summary>
    /// Длительность крафта по рецепту.
    /// </summary>
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Предмет, который появится вместо исходного.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Result;
}
