using Content.Shared._Lust.Smell.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Components;

[RegisterComponent,NetworkedComponent,AutoGenerateComponentState]
public sealed partial class ScentComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<ScentPrototype>> BaseScents = new();

    [DataField]
    public ProtoId<PersonalScentProfilePrototype>? PersonalScentProfile;

    /// <summary>
    /// Временные запахи: события добавили запись, а протухание и силу
    /// вычисляем лениво при запросе. Не сетевой — считается на сервере.
    /// </summary>
    [DataField]
    public List<ActiveTemporaryScent> TemporaryScents = new();

}
