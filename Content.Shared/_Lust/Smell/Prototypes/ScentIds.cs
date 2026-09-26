using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Prototypes;

/// <summary>
/// Scent prototype ids referenced from C# (see scents.yml). Ids used only as
/// data in item/status YAML are intentionally absent and stay there:
/// LustAlcohol, LustDrug, LustStimulant and LustGunpowder are set directly
/// in the emitter/statusScent prototypes.
/// </summary>
public static class ScentIds
{
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

    /// <summary>
    /// Smoke smell from burning tobacco products. Also set in item YAML for the
    /// equip trigger, which is a separate path from the ignite handler in C#.
    /// </summary>
    [ValidatePrototypeId<ScentPrototype>]
    public const string Smoke = "LustSmoke";

    /// <summary>Pheromone scent of an aroused body; the description is chosen per observer.</summary>
    [ValidatePrototypeId<ScentPrototype>]
    public const string Arousal = "LustArousal";

    /// <summary>Scent left by an orgasm on both participants.</summary>
    [ValidatePrototypeId<ScentPrototype>]
    public const string Orgasm = "LustOrgasm";
}
