using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Swarmer.Components;

/// <summary>
/// Lets a step trigger go off for any living being except swarmers, and drains cyborgs that trip it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SwarmerTrapComponent : Component
{
    /// <summary>
    /// Fraction of a cyborg's maximum battery charge drained when it trips the trap.
    /// </summary>
    [DataField]
    public float BorgDrain = 0.5f;

    /// <summary>
    /// How long a cyborg that trips the trap stays stunned.
    /// </summary>
    [DataField]
    public TimeSpan BorgStunDuration = TimeSpan.FromSeconds(1);
}
