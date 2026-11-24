using Content.Shared.Actions;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Borgs.Components;

/// <summary>
/// Stores the sex used by the interactions panel for a cyborg.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BorgSexSystem))]
public sealed partial class BorgSexComponent : Component
{
    /// <summary>
    /// Stores the selected sex used by interactions.
    /// </summary>
    [AutoNetworkedField]
    public Sex Sex = Sex.Unsexed;
}

public sealed partial class BorgCycleSexActionEvent : InstantActionEvent;

[ByRefEvent]
public record struct BorgSexChangedEvent;
