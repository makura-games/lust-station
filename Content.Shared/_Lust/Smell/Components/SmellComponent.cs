using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Smell.Components;

[RegisterComponent,NetworkedComponent,AutoGenerateComponentState]
public sealed partial class SmellComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool SmellBlocked = false;
}
