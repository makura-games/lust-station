using Content.Shared.Inventory;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.DmArena;

[DataDefinition]
public sealed partial class DmArenaStoreItem
{
    /// <summary>
    /// Набор снаряжения, который выдаётся при покупке.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<StartingGearPrototype> Gear;

    /// <summary>
    /// Ключ локализации названия предмета.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Ключ локализации описания предмета.
    /// </summary>
    [DataField]
    public LocId? Description;

    /// <summary>
    /// Ключ локализации категории магазина.
    /// </summary>
    [DataField(required: true)]
    public LocId Category;

    /// <summary>
    /// Ключ локализации группы внутри категории.
    /// </summary>
    [DataField]
    public LocId? Group;

    /// <summary>
    /// Прототип сущности, спрайт которой показывается на карточке.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Sprite;

    /// <summary>
    /// Цена предмета в очках бюджета.
    /// </summary>
    [DataField(required: true)]
    public int Cost;

    /// <summary>
    /// Выдавать предмет раньше остальных.
    /// </summary>
    [DataField]
    public bool EquipFirst;

    /// <summary>
    /// Выдавать предмет в руки.
    /// </summary>
    [DataField]
    public bool InHand;

    /// <summary>
    /// Сколько раз можно купить предмет.
    /// </summary>
    [DataField]
    public int MaxCount = 1;

    /// <summary>
    /// Тип боеприпасов, которые использует это оружие.
    /// </summary>
    [DataField]
    public string? AmmoType;

    /// <summary>
    /// Тип оружия, для которого эти боеприпасы. Значение base означает базовое оружие.
    /// </summary>
    [DataField]
    public string? AmmoFor;
}

[DataDefinition]
public sealed partial class DmArenaStoreSet
{
    /// <summary>
    /// Ключ локализации названия набора.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Ключ локализации описания набора.
    /// </summary>
    [DataField]
    public LocId? Description;

    /// <summary>
    /// Наборы снаряжения, входящие в набор.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<StartingGearPrototype>> Items = new();
}

[RegisterComponent]
public sealed partial class DmArenaRuleComponent : Component
{
    /// <summary>
    /// Карта арены, пока она загружена.
    /// </summary>
    public MapId? ArenaMap;

    /// <summary>
    /// Гриды арены.
    /// </summary>
    public HashSet<EntityUid> ArenaGrids = new();

    /// <summary>
    /// Участники, которые сейчас на арене.
    /// </summary>
    public HashSet<EntityUid> Players = new();

    /// <summary>
    /// Идёт ли сейчас арена.
    /// </summary>
    public bool Active;

    /// <summary>
    /// Сохранённые комплекты игроков, как индексы предметов магазина.
    /// </summary>
    public Dictionary<NetUserId, List<int>> Presets = new();

    /// <summary>
    /// Последний выбранный комплект игрока, живёт до конца раунда.
    /// </summary>
    [NonSerialized]
    public Dictionary<NetUserId, List<int>> LastLoadouts = new();

    /// <summary>
    /// Время следующей уборки арены.
    /// </summary>
    public TimeSpan NextCleanup;

    /// <summary>
    /// Время, до которого игрок не может вернуться на арену.
    /// </summary>
    public Dictionary<NetUserId, TimeSpan> RespawnCooldowns = new();

    /// <summary>
    /// Базовое снаряжение каждого бойца.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype> Gear = "LustDmArenaBaseGear";

    /// <summary>
    /// Предметы магазина.
    /// </summary>
    [DataField]
    public List<DmArenaStoreItem> ShopItems = new();

    /// <summary>
    /// Готовые наборы снаряжения.
    /// </summary>
    [DataField]
    public List<DmArenaStoreSet> Sets = new();

    /// <summary>
    /// Бюджет очков на один комплект.
    /// </summary>
    [DataField]
    public int Budget = 100;

    /// <summary>
    /// Как часто арена убирается.
    /// </summary>
    [DataField]
    public TimeSpan CleanupInterval = TimeSpan.FromSeconds(25);

    /// <summary>
    /// Через сколько секунд исчезают брошенные предметы.
    /// </summary>
    [DataField]
    public float SweepDespawnTime = 10f;

    /// <summary>
    /// Задержка перед повторным входом после смерти.
    /// </summary>
    [DataField]
    public TimeSpan RespawnCooldown = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Сколько после боя нельзя выйти с арены.
    /// </summary>
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

    /// <summary>
    /// Слоты, снаряжение из которых нельзя снимать с бойца.
    /// </summary>
    [DataField]
    public SlotFlags LockedSlots = SlotFlags.OUTERCLOTHING | SlotFlags.HEAD | SlotFlags.BELT | SlotFlags.BACK | SlotFlags.SUITSTORAGE;
}
