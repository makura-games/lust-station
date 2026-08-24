using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Prototypes;

/// <summary>
/// Профиль личного аромата расы/существа: пулы нот, из которых генератор
/// по seed персонажа выбирает по одной ноте на пул.
/// </summary>
[Prototype]
public sealed partial class PersonalScentProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Пулы нот: из каждого берётся ровно одна нота.
    /// </summary>
    [DataField(required: true)]
    public List<ScentNotePool> NotePools { get; private set; } = [];
}

/// <summary>
/// Пул взаимозаменяемых нот одного слоя аромата (база/природа/акцент).
/// </summary>
[DataDefinition]
public sealed partial class ScentNotePool
{
    /// <summary>
    /// Варианты нот слоя.
    /// </summary>
    [DataField(required: true)]
    public List<LocId> Notes { get; private set; } = [];
}
