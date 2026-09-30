using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Kitsune;

/// <summary>
/// Component that tracks Kitsune transformation state.
/// Attached to humanoid entities that are Kitsune species.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class KitsuneTransformComponent : Component
{
    /// <summary>
    /// The stashed humanoid entity when transformed into fox form.
    /// </summary>
    [ViewVariables]
    public EntityUid? StashedHumanoid = null;

    /// <summary>
    /// Actions granted by this component.
    /// Moved here from ActionGrant to prevent loss during anomaly infection.
    /// </summary>
    [DataField]
    public List<EntProtoId> Actions = new();

    [ViewVariables]
    public List<EntityUid> ActionEntities = new();

    /// <summary>
    /// How long the fox transformation lasts (seconds)
    /// </summary>
    [DataField("duration")]
    public float Duration = 240f;

    /// <summary>
    /// Duration of the do-after for transformation (seconds)
    /// </summary>
    [DataField("delay")]
    public float Delay = 3f;
}
