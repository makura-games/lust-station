using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Swarmer;

[Serializable, NetSerializable]
public enum SwarmerVisuals : byte
{
    /// <summary>
    /// Whether a player is controlling the swarmer. Inactive swarmers show their dormant sprite.
    /// </summary>
    Active,
}

[Serializable, NetSerializable]
public enum SwarmerVisualLayers : byte
{
    Body,
    Dormant,
}
