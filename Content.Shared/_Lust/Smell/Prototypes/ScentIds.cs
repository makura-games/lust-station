using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Prototypes;

/// <summary>
/// Single source of scent prototype ids used by the server-side smell logic
/// (references into scents.yml). Item emitter and status-scent ids are not
/// duplicated here — those are set directly in the item YAML.
/// </summary>
public static class ScentIds
{
    /// <summary>Arousal pheromones emitted while an entity is aroused.</summary>
    [ValidatePrototypeId<ScentPrototype>]
    public const string Arousal = "LustArousal";

    /// <summary>Musk left on participants after an orgasm.</summary>
    [ValidatePrototypeId<ScentPrototype>]
    public const string Orgasm = "LustOrgasm";

    /// <summary>The entity's own blood, smelled once wound damage crosses the threshold.</summary>
    [ValidatePrototypeId<ScentPrototype>]
    public const string Blood = "LustBlood";

    /// <summary>Victim's blood smeared onto the attacker finishing off a critical target.</summary>
    [ValidatePrototypeId<ScentPrototype>]
    public const string OtherBlood = "LustOtherBlood";

    /// <summary>Adrenaline sweat smelled once blunt damage crosses the threshold.</summary>
    [ValidatePrototypeId<ScentPrototype>]
    public const string Bruise = "LustBruise";

    /// <summary>Toxic odor smelled once poison damage crosses the threshold.</summary>
    [ValidatePrototypeId<ScentPrototype>]
    public const string Poison = "LustPoison";
}
