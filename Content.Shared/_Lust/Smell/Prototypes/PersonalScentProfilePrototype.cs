using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Prototypes;

[Prototype]
public sealed partial class PersonalScentProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ScentNotePool> NotePools { get; private set; } = [];
}

[DataDefinition]
public sealed partial class ScentNotePool
{
    [DataField(required: true)]
    public List<LocId> Notes { get; private set; } = [];
}
