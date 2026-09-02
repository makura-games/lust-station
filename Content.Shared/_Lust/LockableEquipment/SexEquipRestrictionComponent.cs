using Content.Shared.Humanoid;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.LockableEquipment;

/// <summary>
/// Restricts equipping this item to entities with specific sex values.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SexEquipRestrictionComponent : Component
{
    /// <summary>
    /// Sex values that are allowed to equip this item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Sex> AllowedSexes = new();
}
