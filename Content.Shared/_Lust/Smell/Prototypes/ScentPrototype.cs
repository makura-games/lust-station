using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Prototypes;

[Prototype("scent")]
public sealed partial class ScentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Description { get; private set; } = default!;

    [DataField]
    public Color? Color { get; private set; }
}
