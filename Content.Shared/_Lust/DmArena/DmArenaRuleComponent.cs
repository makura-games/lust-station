using Content.Shared.Inventory;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.DmArena;

[DataDefinition]
public sealed partial class DmArenaStoreItem
{
    [DataField(required: true)]
    public ProtoId<StartingGearPrototype> Gear;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId? Description;

    [DataField(required: true)]
    public LocId Category;

    [DataField]
    public LocId? Group;

    [DataField(required: true)]
    public EntProtoId Sprite;

    [DataField(required: true)]
    public int Cost;

    [DataField]
    public bool EquipFirst;

    [DataField]
    public bool InHand;

    [DataField]
    public int MaxCount = 1;

    [DataField]
    public string? AmmoType;

    // "base" доступно всегда
    [DataField]
    public string? AmmoFor;
}

[DataDefinition]
public sealed partial class DmArenaStoreSet
{
    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId? Description;

    [DataField(required: true)]
    public List<ProtoId<StartingGearPrototype>> Items = new();
}

[RegisterComponent]
public sealed partial class DmArenaRuleComponent : Component
{
    public MapId? ArenaMap;

    public HashSet<EntityUid> ArenaGrids = new();

    public HashSet<NetEntity> Players = new();

    public bool Active;

    // Пресет живёт до конца раунда
    public Dictionary<NetUserId, List<int>> Presets = new();

    /// <summary>
    /// Последний выбранный комплект игрока, живёт до конца раунда.
    /// </summary>
    [NonSerialized]
    public Dictionary<NetUserId, List<int>> LastLoadouts = new();

    public TimeSpan NextCleanup;

    public Dictionary<NetUserId, TimeSpan> RespawnCooldowns = new();

    [DataField]
    public ProtoId<StartingGearPrototype> Gear = "LustDmArenaBaseGear";

    [DataField]
    public List<DmArenaStoreItem> ShopItems = new();

    [DataField]
    public List<DmArenaStoreSet> Sets = new();

    [DataField]
    public int Budget = 100;

    [DataField]
    public TimeSpan CleanupInterval = TimeSpan.FromSeconds(25);

    [DataField]
    public float SweepDespawnTime = 10f;

    [DataField]
    public TimeSpan RespawnCooldown = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan CombatLock = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Множитель яркости света на арене.
    /// </summary>
    [DataField]
    public float LightMultiplier = 1.15f;

    /// <summary>
    /// Множитель задержки лечения на арене.
    /// </summary>
    [DataField]
    public float HealingDelayMultiplier = 0.5f;

    /// <summary>
    /// Существо, в которое превращается игрок при включённом режиме rofls.
    /// </summary>
    [DataField]
    public EntProtoId RoflsPrototype = "MobPig";

    [DataField]
    public SlotFlags LockedSlots = SlotFlags.OUTERCLOTHING | SlotFlags.HEAD | SlotFlags.BELT | SlotFlags.BACK | SlotFlags.SUITSTORAGE;
}
