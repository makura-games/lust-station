using Content.Shared._Lust.Smell.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Components;

[RegisterComponent,NetworkedComponent,AutoGenerateComponentState]
public sealed partial class ScentComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<ScentPrototype>> BaseScents = new();

}
