using Content.Shared.Alert;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Materials;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Swarmer.Components;

/// <summary>
/// Marks an entity as a swarmer: a self-replicating machine that consumes the station.
/// The gameplay concept is inspired by the swarmer antagonist from /tg/station; the implementation is original.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SwarmerComponent : Component
{
    #region Body

    /// <summary>
    /// Damage dealt to the swarmer when it is hit by an EMP. Swarmers are extremely vulnerable to EMPs.
    /// </summary>
    [DataField]
    public DamageSpecifier EmpDamage = new();

    /// <summary>
    /// How long the swarmer has to stand still to fully repair itself.
    /// </summary>
    [DataField]
    public TimeSpan SelfRepairDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Fraction of a cyborg's maximum battery charge drained by each melee hit.
    /// Cyborgs have no stamina, so this is how swarmers wear them down instead.
    /// </summary>
    [DataField]
    public float BorgDrainPerHit = 0.2f;

    /// <summary>
    /// Effect spawned on everything a swarmer hits.
    /// </summary>
    [DataField]
    public EntProtoId HitEffect = "EffectSparks";

    /// <summary>
    /// Body sprite state shown while the swarmer is moving.
    /// </summary>
    [DataField]
    public string MovingState = "swarmer-move";

    /// <summary>
    /// Body sprite state shown while the swarmer stands still.
    /// </summary>
    [DataField]
    public string IdleState = "swarmer";

    #endregion

    #region Resources

    /// <summary>
    /// Resources gathered by integrating items. Spent on structures, replication and escaping.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Resources = 20;

    /// <summary>
    /// The most resources the swarmer can hold.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxResources = 100;

    /// <summary>
    /// Alert showing the resource counter.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> ResourceAlert = "SwarmerResources";

    /// <summary>
    /// Resources gained per integrated item (or per single item from a stack).
    /// </summary>
    [DataField]
    public int IntegrateGain = 1;

    /// <summary>
    /// An item must contain at least one of these materials to be integrated.
    /// </summary>
    [DataField]
    // Не collection expression: непустой компилируется в CollectionsMarshal.SetCount, а песочница клиента его запрещает.
    public List<ProtoId<MaterialPrototype>> IntegrateMaterials = new() { "Steel", "Glass" };

    /// <summary>
    /// Items that can never be integrated, e.g. guns, so swarmers don't empty the armory.
    /// </summary>
    [DataField]
    public EntityWhitelist? IntegrateBlacklist;

    /// <summary>
    /// Items that can be integrated even though they list no materials, e.g. glass shards.
    /// </summary>
    [DataField]
    public EntityWhitelist? IntegrateWhitelist;

    /// <summary>
    /// Effect spawned on an integrated item.
    /// </summary>
    [DataField]
    public EntProtoId IntegrateEffect = "EffectSwarmerIntegrate";

    #endregion

    #region Disintegration

    /// <summary>
    /// How long it takes to disintegrate a structure (walls, windows, doors, furniture...).
    /// </summary>
    [DataField]
    public TimeSpan DisintegrateDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Effect spawned on a structure when it's disintegrated.
    /// </summary>
    [DataField]
    public EntProtoId DisintegrateEffect = "EffectSwarmerDisintegrate";

    /// <summary>
    /// How long it takes to dismantle a machine or computer.
    /// </summary>
    [DataField]
    public TimeSpan DismantleDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Effect spawned on a machine when the swarmer starts dismantling it.
    /// </summary>
    [DataField]
    public EntProtoId DismantleEffect = "EffectSwarmerDismantle";

    /// <summary>
    /// What a dismantled machine leaves behind, besides its board and parts.
    /// </summary>
    [DataField]
    public EntProtoId DismantleLoot = "SheetSteel1";

    /// <summary>
    /// Stack size of <see cref="DismantleLoot"/>.
    /// </summary>
    [DataField]
    public int DismantleLootAmount = 5;

    /// <summary>
    /// Structures that break apart the way they normally would when destroyed (a wall leaves a girder and its material)
    /// instead of vanishing. Their loot is guaranteed: every possible drop spawns once.
    /// </summary>
    [DataField]
    public EntityWhitelist? SalvageWhitelist;

    /// <summary>
    /// Same as <see cref="SalvageWhitelist"/>, but matched by construction graph (girders have no tags of their own).
    /// </summary>
    [DataField]
    public List<ProtoId<ConstructionGraphPrototype>> SalvageGraphs = [];

    /// <summary>
    /// Extra loot left by disintegrated structures, e.g. airlocks leave steel.
    /// </summary>
    [DataField]
    public List<SwarmerExtraLoot> ExtraLoot = [];

    /// <summary>
    /// Walls, windows and doors can't be removed if pressure around them differs by more than this, in kPa.
    /// </summary>
    [DataField]
    public float MaxPressureDelta = 250f;

    /// <summary>
    /// Things swarmers refuse to destroy, each with the reason shown to the swarmer.
    /// Checked in order, the first match wins.
    /// </summary>
    [DataField]
    public List<SwarmerForbiddenTarget> ForbiddenTargets = [];

    #endregion

    #region Teleports

    /// <summary>
    /// How long a swarmer has to hold a victim to teleport it away.
    /// </summary>
    [DataField]
    public TimeSpan TeleportDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Restraints put on teleported victims.
    /// </summary>
    [DataField]
    public EntProtoId? TeleportCuffs = "SwarmerEnergyCuffs";

    /// <summary>
    /// Played where a teleported entity (a victim, or the swarmer itself when escaping) disappears.
    /// </summary>
    [DataField]
    public SoundSpecifier TeleportDepartureSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");

    /// <summary>
    /// Played where a teleported entity reappears.
    /// </summary>
    [DataField]
    public SoundSpecifier TeleportArrivalSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    /// <summary>
    /// Effect spawned on both ends of a teleport.
    /// </summary>
    [DataField]
    public EntProtoId TeleportEffect = "EffectSparks";

    /// <summary>
    /// Cost of the emergency jump to a random safe spot on the station.
    /// </summary>
    [DataField]
    public int EscapeCost = 10;

    #endregion

    #region Replication

    /// <summary>
    /// What replication produces: a full swarmer that stays dormant until a ghost takes it over.
    /// </summary>
    [DataField]
    public EntProtoId ReplicaPrototype = "MobSwarmer";

    /// <summary>
    /// Resources spent on one replica.
    /// </summary>
    [DataField]
    public int ReplicateCost = 20;

    /// <summary>
    /// How long the swarmer has to stand still to build a replica.
    /// </summary>
    [DataField]
    public TimeSpan ReplicateDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Resources a fresh replica starts with. Zero, so replicating never creates resources out of nothing.
    /// </summary>
    [DataField]
    public int ReplicaStartingResources;

    #endregion

    #region Structures

    /// <summary>
    /// Energy barricade built by the swarmer.
    /// </summary>
    [DataField]
    public EntProtoId BarricadePrototype = "SwarmerBarricade";

    /// <summary>
    /// Resources spent on one barricade.
    /// </summary>
    [DataField]
    public int BarricadeCost = 4;

    /// <summary>
    /// How long building a barricade takes.
    /// </summary>
    [DataField]
    public TimeSpan BarricadeDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Shock trap built by the swarmer.
    /// </summary>
    [DataField]
    public EntProtoId TrapPrototype = "SwarmerTrap";

    /// <summary>
    /// Resources spent on one trap.
    /// </summary>
    [DataField]
    public int TrapCost = 4;

    #endregion
}

/// <summary>
/// Loot dropped when a swarmer disintegrates a matching structure.
/// </summary>
[DataDefinition]
public sealed partial class SwarmerExtraLoot
{
    /// <summary>
    /// Structures this loot comes from.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    /// <summary>
    /// Stack prototype that is spawned.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Spawn;

    /// <summary>
    /// Stack size of the spawned loot.
    /// </summary>
    [DataField]
    public int Amount = 1;
}

/// <summary>
/// A kind of target swarmers refuse to take apart.
/// </summary>
[DataDefinition]
public sealed partial class SwarmerForbiddenTarget
{
    /// <summary>
    /// Entities this rule applies to.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    /// <summary>
    /// Why the swarmer refuses, shown to it as a popup. Gets the target as <c>$target</c>.
    /// </summary>
    [DataField(required: true)]
    public LocId Message;
}
