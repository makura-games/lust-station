using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.EnergyDome;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Dataset;
using Content.Shared.EnergyDome;
using Content.Shared.Examine;
using Content.Shared.Fluids.Components;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Medical.Healing;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared._Lust.DmArena;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server._Lust.DmArena;

public sealed partial class DmArenaMatchSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private EuiManager _euiManager = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private DmArenaMindSwapSystem _mindSwap = default!;
    [Dependency] private GunSystem _gun = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private TagSystem _tags = default!;

    private static readonly EntProtoId RulePrototype = "LustDmArenaRule";
    private static readonly ProtoId<TagPrototype> CatwalkTag = "Catwalk";
    private static readonly ProtoId<LocalizedDatasetPrototype> DmArenaNameSuffixDataset = "NamesOperationPrefix";
    private static readonly SoundPathSpecifier LeaveSound = new("/Audio/Effects/pop_high.ogg");
    private EntityUid? _ruleEntity;
    private bool _enabled = true;
    private bool _openedManually;
    private bool _wasOpen;
    private TimeSpan _openAfter;
    private TimeSpan _nextOpenCheck;
    private TimeSpan _nextCountSync;
    private TimeSpan _openCheckInterval;
    private TimeSpan _countSyncInterval;

    private bool _refillOnKill;

    public bool DebugCombatLock = true;
    public readonly HashSet<string> DebugDisabledItems = new();
    public readonly HashSet<NetUserId> DebugNoCooldown = new();
    public readonly HashSet<NetUserId> DebugRofls = new();

    private readonly Dictionary<ICommonSession, DmArenaKitEui> _activeEuis = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, DmArenaConfigVars.DmArenaRefill, value => _refillOnKill = value, true);
        Subs.CVar(_cfg, DmArenaConfigVars.DmArenaEnabled, OnEnabledChanged, true);
        Subs.CVar(_cfg, DmArenaConfigVars.DmArenaOpenCheckInterval, value => _openCheckInterval = TimeSpan.FromSeconds(value), true);
        Subs.CVar(_cfg, DmArenaConfigVars.DmArenaCountSyncInterval, value => _countSyncInterval = TimeSpan.FromSeconds(value), true);
        Subs.CVar(_cfg, DmArenaConfigVars.DmArenaOpenDelay, value => _openAfter = TimeSpan.FromMinutes(value), true);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundEnding);
        SubscribeLocalEvent<DmArenaRuleComponent, RuleLoadedGridsEvent>(OnGridsLoaded);
        SubscribeNetworkEvent<DmArenaJoinRequestEvent>(OnJoinRequest);
        SubscribeNetworkEvent<DmArenaLeaveRequestEvent>(OnLeaveRequest);
        SubscribeLocalEvent<DmArenaFighterComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DmArenaDormantBodyComponent, MobStateChangedEvent>(OnOriginalBodyStateChanged);
        SubscribeNetworkEvent<DmArenaRevivalAcceptEvent>(OnRevivalAccept);
        SubscribeNetworkEvent<DmArenaExitChoiceEvent>(OnExitChoice);
        SubscribeLocalEvent<DmArenaFighterComponent, SuicideGhostEvent>(OnSuicideAttempt);
        SubscribeLocalEvent<GhostAttemptHandleEvent>(OnGhostAttempt);
        SubscribeLocalEvent<DmArenaShieldedComponent, BeforeDamageChangedEvent>(OnArenaEntityDamage);
        SubscribeLocalEvent<DamageableComponent, ComponentStartup>(OnDamageableStartup);
        SubscribeLocalEvent<DmArenaShieldedComponent, ToolUseAttemptEvent>(OnArenaEntityToolUse);
        SubscribeLocalEvent<DmArenaFighterComponent, ToolUserAttemptUseEvent>(OnFighterToolUse);
        SubscribeLocalEvent<DmArenaMapItemComponent, GettingPickedUpAttemptEvent>(OnMapItemPickup);
        SubscribeLocalEvent<DmArenaMapItemComponent, GettingInteractedWithAttemptEvent>(OnMapItemInteract);
        SubscribeLocalEvent<DmArenaMapItemComponent, PullAttemptEvent>(OnMapItemPull);
        SubscribeLocalEvent<DmArenaSweptComponent, EntGotInsertedIntoContainerMessage>(OnDespawnPickedUp);
        SubscribeLocalEvent<DmArenaDormantBodyComponent, ExaminedEvent>(OnOriginalBodyExamined);
        SubscribeLocalEvent<DmArenaFighterComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<DmArenaFighterComponent, EntityTerminatingEvent>(OnFighterTerminating);
        SubscribeLocalEvent<DmArenaFighterComponent, DamageDealtEvent>(OnFighterDamaged);
        SubscribeLocalEvent<EnergyDomeComponent, DamageDealtEvent>(OnDomeDamaged);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DmArenaFighterComponent, ThrowHitByEvent>(OnFighterThrownHit);
        SubscribeLocalEvent<DmArenaFighterComponent, KnockDownAttemptEvent>(OnFighterKnockDown);
        SubscribeLocalEvent<DmArenaFighterComponent, IsUnequippingTargetAttemptEvent>(OnFighterUnequip);

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
        {
            DebugNoCooldown.Remove(args.Session.UserId);
            DebugRofls.Remove(args.Session.UserId);
            return;
        }

        if (args.NewStatus != SessionStatus.InGame)
            return;

        RaiseNetworkEvent(new DmArenaPlayerCountEvent(CountFighters(), IsOpen), args.Session.Channel);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        if (HasComp<GhostComponent>(args.Entity))
            RaiseNetworkEvent(new DmArenaPlayerCountEvent(CountFighters(), IsOpen), args.Player.Channel);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime >= _nextOpenCheck)
        {
            _nextOpenCheck = _timing.CurTime + _openCheckInterval;
            RefreshOpenState();
        }

        if (_timing.CurTime >= _nextCountSync)
        {
            _nextCountSync = _timing.CurTime + _countSyncInterval;
            SendPlayerCount();
        }

        if (CurrentRule is not { Active: true } rule || _timing.CurTime < rule.NextCleanup)
            return;

        rule.NextCleanup = _timing.CurTime + rule.CleanupInterval;
        SweepLooseItems(rule);
    }

    public IEnumerable<string> ShopGearIds
    {
        get
        {
            var items = CurrentRule?.ShopItems;
            if (items == null && _prototype.Index(RulePrototype).TryGetComponent<DmArenaRuleComponent>(out var rule, _componentFactory))
                items = rule.ShopItems;

            return items?.Select(x => x.Gear.Id) ?? Enumerable.Empty<string>();
        }
    }

    public void ResetDebug()
    {
        DebugCombatLock = true;
        DebugDisabledItems.Clear();
        DebugNoCooldown.Clear();
        DebugRofls.Clear();
        _cfg.SetCVar(DmArenaConfigVars.DmArenaRefill, DmArenaConfigVars.DmArenaRefill.DefaultValue);
    }

    private bool IsOpen => _enabled
        && (_openedManually || _ticker.RunLevel == GameRunLevel.InRound && _ticker.RoundDuration() >= _openAfter);

    public void OpenManually()
    {
        _openedManually = true;
        RefreshOpenState();
    }

    private void RefreshOpenState()
    {
        var open = IsOpen;
        if (open == _wasOpen)
            return;

        _wasOpen = open;
        BroadcastPlayerCount();
    }

    private DmArenaRuleComponent? CurrentRule =>
        _ruleEntity is { } uid ? CompOrNull<DmArenaRuleComponent>(uid) : null;

    private bool TryGetFighterRule(Entity<DmArenaFighterComponent> fighter, [NotNullWhen(true)] out DmArenaRuleComponent? rule)
    {
        rule = fighter.Comp.RuleEntity is { } uid ? CompOrNull<DmArenaRuleComponent>(uid) : null;
        return rule != null;
    }

    private bool IsIdleGhost([NotNullWhen(true)] EntityUid? uid)
    {
        return uid is { Valid: true } ghost && HasComp<GhostComponent>(ghost) && !HasComp<DmArenaFighterComponent>(ghost);
    }

    private void EnsureRule()
    {
        if (_ruleEntity is { } existing && !TerminatingOrDeleted(existing))
            return;

        if (_ticker.StartGameRule(RulePrototype, out var started))
            _ruleEntity = started;
        else
            Log.Error($"Failed to start DM arena rule {RulePrototype}");
    }

    public void OnEuiClosed(ICommonSession session, DmArenaKitEui eui)
    {
        if (_activeEuis.TryGetValue(session, out var current) && current == eui)
            _activeEuis.Remove(session);
    }

    private void CloseEuis()
    {
        foreach (var (session, eui) in _activeEuis.ToList())
        {
            if (session.Status != SessionStatus.Disconnected)
                eui.Close();
        }

        _activeEuis.Clear();
    }

    private void OnRoundEnding(RoundRestartCleanupEvent ev)
    {
        CloseEuis();
        ResetDebug();
        _openedManually = false;

        if (CurrentRule is { } rule)
        {
            var fighters = EntityQueryEnumerator<DmArenaFighterComponent>();
            while (fighters.MoveNext(out var uid, out _))
            {
                _mindSwap.TryRestoreAsGhost(uid);
                QueueDel(uid);
            }

            rule.Players.Clear();
            rule.Presets.Clear();
            rule.LastLoadouts.Clear();
            rule.Active = false;
            BroadcastPlayerCount();
        }
        else if (_ruleEntity == null)
        {
            return;
        }

        var dormant = EntityQueryEnumerator<DmArenaDormantBodyComponent>();
        while (dormant.MoveNext(out var uid, out _))
        {
            RemComp<DmArenaDormantBodyComponent>(uid);
        }

        _ruleEntity = null;
    }

    private void OnGridsLoaded(Entity<DmArenaRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        ent.Comp.ArenaMap = args.Map;
        ent.Comp.ArenaGrids.UnionWith(args.Grids);
        ent.Comp.Active = true;
        ShieldArenaEntities(args.Map);
        MarkMapItems(args.Map);
        BrightenLights(args.Map, ent.Comp.LightMultiplier);
        BroadcastPlayerCount();
    }

    private void ShieldArenaEntities(MapId map)
    {
        var query = EntityQueryEnumerator<DamageableComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID == map)
                ShieldEntity(uid);
        }
    }

    private void BrightenLights(MapId map, float multiplier)
    {
        var lights = EntityQueryEnumerator<PointLightComponent, TransformComponent>();
        while (lights.MoveNext(out var uid, out var light, out var xform))
        {
            if (xform.MapID == map)
                _light.SetEnergy(uid, light.Energy * multiplier, light);
        }
    }

    private void MarkMapItems(MapId map)
    {
        var items = EntityQueryEnumerator<ItemComponent, TransformComponent>();
        while (items.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID == map)
                EnsureComp<DmArenaMapItemComponent>(uid);
        }

        var puddles = EntityQueryEnumerator<PuddleComponent, TransformComponent>();
        while (puddles.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID == map)
                EnsureComp<DmArenaMapItemComponent>(uid);
        }

        var lights = EntityQueryEnumerator<FixturesComponent, MetaDataComponent, TransformComponent>();
        while (lights.MoveNext(out var uid, out var fixtures, out var meta, out var xform))
        {
            if (xform.MapID != map || meta.EntityPrototype?.ID != "AlwaysPoweredFloorLight")
                continue;

            foreach (var (id, fixture) in fixtures.Fixtures)
            {
                _physics.SetCollisionLayer(uid, id, fixture, fixture.CollisionLayer & ~(int)CollisionGroup.BulletImpassable, fixtures);
            }
        }

        var bridgeWater = new List<EntityUid>();
        var water = EntityQueryEnumerator<SpeedModifierContactsComponent, MetaDataComponent, TransformComponent>();
        while (water.MoveNext(out var uid, out _, out var meta, out var xform))
        {
            if (xform.MapID != map || meta.EntityPrototype?.ID != "FloorWaterEntity" || xform.GridUid is not { } grid)
                continue;

            foreach (var anchored in _map.GetAnchoredEntities((grid, Comp<MapGridComponent>(grid)), xform.Coordinates))
            {
                if (_tags.HasTag(anchored, CatwalkTag))
                {
                    bridgeWater.Add(uid);
                    break;
                }
            }
        }

        foreach (var uid in bridgeWater)
        {
            RemComp<SpeedModifierContactsComponent>(uid);
        }
    }

    private void OnMapItemPickup(Entity<DmArenaMapItemComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnMapItemInteract(Entity<DmArenaMapItemComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMapItemPull(Entity<DmArenaMapItemComponent> ent, ref PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnDamageableStartup(Entity<DamageableComponent> ent, ref ComponentStartup args)
    {
        if (CurrentRule?.ArenaMap is { } map && Transform(ent).MapID == map)
            ShieldEntity(ent);
    }

    private void ShieldEntity(EntityUid uid)
    {
        if (!HasComp<MobStateComponent>(uid) && !HasComp<EnergyDomeComponent>(uid))
            EnsureComp<DmArenaShieldedComponent>(uid);
    }

    private void OnFighterToolUse(Entity<DmArenaFighterComponent> ent, ref ToolUserAttemptUseEvent args)
    {
        if (args.Target is not { } target || !HasComp<ToolTileCompatibleComponent>(target))
            return;

        _popup.PopupEntity(Loc.GetString("dm-arena-indestructible"), ent.Owner, ent.Owner);
        args.Cancelled = true;
    }

    private void OnArenaEntityToolUse(EntityUid uid, DmArenaShieldedComponent component, ToolUseAttemptEvent args)
    {
        if (HasComp<ToolComponent>(uid))
            return;

        _popup.PopupEntity(Loc.GetString("dm-arena-indestructible"), uid, args.User);
        args.Cancel();
    }

    private static void OnArenaEntityDamage(Entity<DmArenaShieldedComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }

    private void OnJoinRequest(DmArenaJoinRequestEvent ev, EntitySessionEventArgs args)
    {
        TryOpenLoadout(args.SenderSession);
    }

    public bool TryOpenLoadout(ICommonSession session)
    {
        if (!IsOpen)
        {
            RaiseNetworkEvent(new DmArenaPlayerCountEvent(0, false), session.Channel);
            return false;
        }

        EnsureRule();

        if (!CanOpenLoadout(session, out var ruleUid))
            return false;

        OpenLoadout(session, ruleUid);
        return true;
    }

    public bool CanOpenLoadout(ICommonSession session, out EntityUid ruleUid)
    {
        ruleUid = default;

        if (_ruleEntity is not { } rule || CurrentRule is not { Active: true })
        {
            Log.Warning($"DM arena join rejected: rule {ToPrettyString(_ruleEntity)} is not active or the map is not loaded");
            return false;
        }

        if (!IsIdleGhost(session.AttachedEntity))
        {
            Log.Warning($"DM arena join rejected for {session.Name}: {ToPrettyString(session.AttachedEntity)} is not a ghost or is already on the arena");
            return false;
        }

        if (CurrentRule is { } current
            && !DebugNoCooldown.Contains(session.UserId)
            && current.RespawnCooldowns.TryGetValue(session.UserId, out var until)
            && until > _timing.CurTime)
        {
            var seconds = (int) Math.Ceiling((until - _timing.CurTime).TotalSeconds);
            _popup.PopupEntity(Loc.GetString("dm-arena-respawn-cooldown", ("seconds", seconds)), session.AttachedEntity!.Value, session.AttachedEntity.Value);
            return false;
        }

        ruleUid = rule;
        return true;
    }

    private void OpenLoadout(ICommonSession session, EntityUid ruleUid)
    {
        if (_activeEuis.Remove(session, out var previous) && previous.Player.Status != SessionStatus.Disconnected)
            previous.Close();

        var eui = new DmArenaKitEui(this, ruleUid, session);
        _euiManager.OpenEui(eui, session);
        _activeEuis[session] = eui;
    }

    private void OnExitChoice(DmArenaExitChoiceEvent ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!IsIdleGhost(session.AttachedEntity))
            return;

        if (ev.Stay)
            TryOpenLoadout(session);
        else
            TryLeaveToOriginalBody(session.AttachedEntity.Value);
    }

    public bool TryLeaveToOriginalBody(EntityUid ghost)
    {
        if (!CanLeaveToOriginalBody(ghost, out var mindId, out var mind))
            return false;

        _mind.UnVisit(mindId, mind);

        if (!TerminatingOrDeleted(ghost))
            QueueDel(ghost);

        return true;
    }

    public bool CanLeaveToOriginalBody(EntityUid ghost, out EntityUid mindId, [NotNullWhen(true)] out MindComponent? mind)
    {
        return _mind.TryGetMind(ghost, out mindId, out mind)
            && mind.OwnedEntity is { Valid: true } body
            && body != ghost
            && Exists(body);
    }

    private void OnLeaveRequest(DmArenaLeaveRequestEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is { Valid: true } entity
            && TryComp<DmArenaFighterComponent>(entity, out var fighter)
            && !IsInCombat((entity, fighter)))
            LeaveDmArena((entity, fighter));
    }

    private void OnFighterDamaged(Entity<DmArenaFighterComponent> ent, ref DamageDealtEvent args)
    {
        MarkCombat(ent, args.Origin);
    }

    private void OnFighterThrownHit(Entity<DmArenaFighterComponent> ent, ref ThrowHitByEvent args)
    {
        ent.Comp.LastThrownHit = _timing.CurTime;
    }

    private void OnFighterKnockDown(Entity<DmArenaFighterComponent> ent, ref KnockDownAttemptEvent args)
    {
        if (!args.Drop && ent.Comp.LastThrownHit == _timing.CurTime)
            args.Cancelled = true;
    }

    private void OnDomeDamaged(Entity<EnergyDomeComponent> ent, ref DamageDealtEvent args)
    {
        if (ent.Comp.Generator is { } generator
            && TryComp<EnergyDomeGeneratorComponent>(generator, out var generatorComp)
            && generatorComp.DomeParentEntity is { } wearer
            && TryComp<DmArenaFighterComponent>(wearer, out var fighter))
            MarkCombat((wearer, fighter), args.Origin);
    }

    private void MarkCombat(Entity<DmArenaFighterComponent> victim, EntityUid? origin)
    {
        if (origin is not { } attacker || attacker == victim.Owner || !TryComp<DmArenaFighterComponent>(attacker, out var other))
            return;

        if (!_mobState.IsCritical(victim))
        {
            victim.Comp.LastAttacker = attacker;
            victim.Comp.LastCombat = _timing.CurTime;
        }

        if (!_mobState.IsCritical(attacker))
            other.LastCombat = _timing.CurTime;
    }

    private void OnFighterUnequip(EntityUid uid, DmArenaFighterComponent component, IsUnequippingTargetAttemptEvent args)
    {
        if (args.User == uid || !TryGetFighterRule((uid, component), out var rule) || (args.SlotFlags & rule.LockedSlots) == 0)
            return;

        args.Reason = "dm-arena-strip-locked";
        args.Cancel();
    }

    private bool IsInCombat(Entity<DmArenaFighterComponent> ent)
    {
        if (!DebugCombatLock
            || !TryGetFighterRule(ent, out var rule)
            || _mobState.IsCritical(ent)
            || _timing.CurTime >= ent.Comp.LastCombat + rule.CombatLock)
            return false;

        _popup.PopupEntity(Loc.GetString("dm-arena-in-combat"), ent.Owner, ent.Owner);
        return true;
    }

    private void OnMobStateChanged(Entity<DmArenaFighterComponent> ent, ref MobStateChangedEvent args)
    {
        if (!TryGetFighterRule(ent, out var rule))
            return;

        if (args.NewMobState == MobState.Critical)
            ent.Comp.LastCombat = TimeSpan.Zero;

        switch (args.NewMobState)
        {
            case MobState.Critical when args.Origin is { } attacker && HasComp<DmArenaFighterComponent>(attacker):
                ent.Comp.LastAttacker = attacker;
                break;
            case MobState.Dead:
                StartRespawnCooldown(ent, rule);
                RewardKill(ent, args.Origin);
                RemoveFromArena(ent, rule, playSound: false);
                break;
        }
    }

    public bool SpawnPlayer(ICommonSession session, EntityUid ruleEntity, List<int> items)
    {
        if (!IsOpen
            || !TryComp<DmArenaRuleComponent>(ruleEntity, out var rule)
            || !rule.Active
            || session.AttachedEntity is not { Valid: true } ghost
            || !HasComp<GhostComponent>(ghost)
            || !TryGetValidItems(rule, items, out var chosen)
            || GetRandomSpawnPoint(rule) is not { } spawnCoords
            || !_mind.TryGetMind(ghost, out _, out var mind))
            return false;

        var originalBody = mind.OwnedEntity != ghost ? mind.OwnedEntity : null;
        var ownerId = mind.UserId;
        var profile = BuildArenaProfile(mind);
        var rofls = DebugRofls.Contains(session.UserId);
        var mob = rofls
            ? Spawn(rule.RoflsPrototype, spawnCoords)
            : _stationSpawning.SpawnPlayerMob(spawnCoords, null, profile, null);

        if (rofls)
        {
            _mind.MakeSentient(mob);
            if (profile != null)
                _meta.SetEntityName(mob, profile.Name);
        }

        if (!rofls && !TryEquipLoadout(mob, rule, chosen, session))
        {
            QueueDel(mob);
            return false;
        }

        EnsureComp<DmArenaFighterComponent>(mob).RuleEntity = ruleEntity;

        if (!_mindSwap.TrySwapTempMind(session, mob))
        {
            QueueDel(mob);
            return false;
        }

        if (originalBody is { Valid: true } body && !HasComp<DmArenaDormantBodyComponent>(body))
        {
            var marker = EnsureComp<DmArenaDormantBodyComponent>(body);
            if (ownerId is { } userId)
                marker.UserId = userId;
        }

        rule.Players.Add(GetNetEntity(mob));
        rule.LastLoadouts[session.UserId] = new List<int>(items);
        _activeEuis.Remove(session);
        BroadcastPlayerCount();
        return true;
    }

    private HumanoidCharacterProfile? BuildArenaProfile(MindComponent mind)
    {
        if (mind.UserId is not { } userId
            || !_prefs.TryGetCachedPreferences(userId, out var prefs)
            || prefs.SelectedCharacter is not HumanoidCharacterProfile selected)
            return null;

        var suffix = Loc.GetString(_random.Pick(_prototype.Index(DmArenaNameSuffixDataset).Values));
        return selected.WithName($"{suffix} {selected.Name}");
    }

    private bool TryEquipLoadout(EntityUid mob, DmArenaRuleComponent rule, List<(DmArenaStoreItem Item, int Count)> chosen, ICommonSession session)
    {
        try
        {
            _stationSpawning.EquipStartingGear(mob, rule.Gear);

            foreach (var (item, count) in chosen.OrderByDescending(x => x.Item.EquipFirst))
            {
                var kept = ClearReplacedSlots(mob, item.Gear);
                for (var i = 0; i < count; i++)
                    _stationSpawning.EquipStartingGear(mob, item.Gear);

                RestoreDependentSlots(mob, kept);
            }

            ScaleHealingDelay(mob, rule.HealingDelayMultiplier);
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to equip DM arena loadout for {session.Name} on {ToPrettyString(mob)}: {e}");
            return false;
        }
    }

    private void ScaleHealingDelay(EntityUid uid, float multiplier)
    {
        if (TryComp<HealingComponent>(uid, out var healing))
        {
            healing.Delay *= multiplier;
            Dirty(uid, healing);
        }

        var children = Transform(uid).ChildEnumerator;
        while (children.MoveNext(out var child))
            ScaleHealingDelay(child, multiplier);
    }

    private List<(string Slot, EntityUid Item)> ClearReplacedSlots(EntityUid mob, ProtoId<StartingGearPrototype> gear)
    {
        var kept = new List<(string Slot, EntityUid Item)>();
        if (!_inventory.TryGetSlots(mob, out var definitions))
            return kept;

        foreach (var slot in _prototype.Index(gear).Equipment.Keys)
        {
            if (!_inventory.TryGetSlotEntity(mob, slot, out var old))
                continue;

            foreach (var dependent in definitions.Where(x => x.DependsOn == slot))
            {
                if (_inventory.TryGetSlotEntity(mob, dependent.Name, out var content)
                    && _inventory.TryUnequip(mob, dependent.Name, silent: true, force: true))
                    kept.Add((dependent.Name, content.Value));
            }

            _inventory.TryUnequip(mob, slot, silent: true, force: true);
            QueueDel(old);
        }

        return kept;
    }

    private void RestoreDependentSlots(EntityUid mob, List<(string Slot, EntityUid Item)> kept)
    {
        foreach (var (slot, item) in kept)
        {
            if (!_inventory.TryEquip(mob, item, slot, silent: true, force: true))
                QueueDel(item);
        }
    }

    private void OnEnabledChanged(bool value)
    {
        _enabled = value;

        if (!value)
        {
            CloseEuis();

            var fighters = new List<Entity<DmArenaFighterComponent>>();
            var query = EntityQueryEnumerator<DmArenaFighterComponent>();
            while (query.MoveNext(out var uid, out var fighter))
            {
                fighters.Add((uid, fighter));
            }

            fighters.ForEach(LeaveDmArena);
        }

        BroadcastPlayerCount();
    }

    private void StartRespawnCooldown(Entity<DmArenaFighterComponent> ent, DmArenaRuleComponent rule)
    {
        if (CompOrNull<ActorComponent>(ent)?.PlayerSession is { } session && !DebugNoCooldown.Contains(session.UserId))
            rule.RespawnCooldowns[session.UserId] = _timing.CurTime + rule.RespawnCooldown;
    }

    private void RemoveFromArena(Entity<DmArenaFighterComponent> ent, DmArenaRuleComponent rule, bool playSound = true)
    {
        var session = CompOrNull<ActorComponent>(ent)?.PlayerSession;

        if (playSound && _transform.TryGetMapOrGridCoordinates(ent, out var coords))
        {
            var name = Identity.Entity(ent, EntityManager);
            _popup.PopupCoordinates(Loc.GetString("dm-arena-leave-01", ("user", name)), coords.Value, PopupType.LargeCaution);

            var filter = Filter.Pvs(coords.Value, 1, EntityManager, _playerManager);
            _audio.PlayStatic(LeaveSound, filter, coords.Value, true, new AudioParams().WithVolume(3));
        }

        ReleaseFighter(ent, rule);

        if (session != null && _enabled)
            RaiseNetworkEvent(new DmArenaExitOfferEvent(), session.Channel);
    }

    private void ReleaseFighter(Entity<DmArenaFighterComponent> ent, DmArenaRuleComponent rule)
    {
        rule.Players.Remove(GetNetEntity(ent));
        ClearOriginalBodyMarker(ent);
        _mindSwap.TryRestoreAsGhost(ent);
        QueueDel(ent);
        BroadcastPlayerCount();
    }

    private void OnSuicideAttempt(Entity<DmArenaFighterComponent> ent, ref SuicideGhostEvent args)
    {
        if (args.Victim != ent.Owner || !TryGetFighterRule(ent, out var rule))
            return;

        if (IsInCombat(ent))
        {
            args.Handled = true;
            return;
        }

        StartRespawnCooldown(ent, rule);
        RewardKill(ent);
        RemoveFromArena(ent, rule);
        args.Handled = true;
    }

    private void OnGhostAttempt(GhostAttemptHandleEvent args)
    {
        if (args.Mind.CurrentEntity is not { } entity
            || !TryComp<DmArenaFighterComponent>(entity, out var fighter)
            || !TryGetFighterRule((entity, fighter), out var rule))
            return;

        if (IsInCombat((entity, fighter)))
        {
            args.Handled = true;
            return;
        }

        StartRespawnCooldown((entity, fighter), rule);
        RewardKill((entity, fighter));
        RemoveFromArena((entity, fighter), rule);
        args.Handled = true;
        args.Result = true;
    }

    private void RewardKill(Entity<DmArenaFighterComponent> victim, EntityUid? killer = null)
    {
        killer ??= victim.Comp.LastAttacker;

        if (_refillOnKill && killer is { } winner && HasComp<DmArenaFighterComponent>(winner))
            RefillAmmo(winner);
    }

    private void LeaveDmArena(Entity<DmArenaFighterComponent> ent)
    {
        if (TryGetFighterRule(ent, out var rule))
            ReleaseFighter(ent, rule);
    }

    private void OnOriginalBodyStateChanged(Entity<DmArenaDormantBodyComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Dead or MobState.Invalid || args.OldMobState == MobState.Alive)
            return;

        if (_playerManager.TryGetSessionById(ent.Comp.UserId, out var session) && session.AttachedEntity is { Valid: true })
            RaiseNetworkEvent(new DmArenaRevivalOfferEvent(), session.Channel);
    }

    private void OnRevivalAccept(DmArenaRevivalAcceptEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } current)
            return;

        if (!TryReturnToBody(current))
            Log.Warning($"DM arena return to body rejected for {args.SenderSession.Name}: {ToPrettyString(current)}");
    }

    public bool TryReturnToBody(EntityUid current)
    {
        if (!CanReturnToBody(current, out var fighter, out var originalBody))
            return false;

        if (TryGetFighterRule((current, fighter), out var rule))
        {
            rule.Players.Remove(GetNetEntity(current));
            BroadcastPlayerCount();
        }

        _mindSwap.TryRestoreToOriginalBody(current);
        RemComp<DmArenaDormantBodyComponent>(originalBody);
        QueueDel(current);
        return true;
    }

    public bool CanReturnToBody(EntityUid current, [NotNullWhen(true)] out DmArenaFighterComponent? fighter, out EntityUid originalBody)
    {
        originalBody = default;

        if (!TryComp<DmArenaFighterComponent>(current, out fighter)
            || !TryComp<DmArenaMindSwapComponent>(current, out var swap)
            || !TryComp<MindComponent>(swap.OriginalMind, out var origMind)
            || origMind.OwnedEntity is not { } body
            || !Exists(body)
            || !TryComp<MobStateComponent>(body, out var mobState)
            || mobState.CurrentState == MobState.Dead)
            return false;

        originalBody = body;
        return true;
    }

    private void SweepLooseItems(DmArenaRuleComponent rule)
    {
        if (rule.ArenaMap is not { } map)
            return;

        var items = EntityQueryEnumerator<ItemComponent, TransformComponent>();
        while (items.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID == map && !HasComp<DmArenaMapItemComponent>(uid) && !HasComp<MobStateComponent>(uid))
                MarkForDespawn(uid, rule.SweepDespawnTime, checkContainer: true);
        }

        var puddles = EntityQueryEnumerator<PuddleComponent, TransformComponent>();
        while (puddles.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID == map && !HasComp<DmArenaMapItemComponent>(uid))
                MarkForDespawn(uid, rule.SweepDespawnTime);
        }
    }

    private void OnOriginalBodyExamined(Entity<DmArenaDormantBodyComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var dead = CompOrNull<MobStateComponent>(ent)?.CurrentState == MobState.Dead;
        var key = dead ? "comp-mind-examined-dead-and-ssd" : "comp-mind-examined-ssd";
        args.PushMarkup($"[color=yellow]{Loc.GetString(key, ("ent", ent))}[/color]");
    }

    private void OnPlayerDetached(Entity<DmArenaFighterComponent> ent, ref PlayerDetachedEvent args)
    {
        if (args.Player.Status != SessionStatus.Disconnected)
            return;

        var dormant = EntityQueryEnumerator<DmArenaDormantBodyComponent>();
        while (dormant.MoveNext(out var uid, out var marker))
        {
            if (marker.UserId != args.Player.UserId)
                continue;

            RemComp<DmArenaDormantBodyComponent>(uid);
            break;
        }

        if (!TryGetFighterRule(ent, out var rule))
            return;

        rule.Players.Remove(GetNetEntity(ent));
        QueueDel(ent);
        BroadcastPlayerCount();
    }

    private void OnFighterTerminating(Entity<DmArenaFighterComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!TryGetFighterRule(ent, out var rule) || !rule.Players.Remove(GetNetEntity(ent)))
            return;

        ClearOriginalBodyMarker(ent);
        _mindSwap.TryRestoreAsGhost(ent);
        BroadcastPlayerCount();
    }

    private void ClearOriginalBodyMarker(EntityUid tempBody)
    {
        if (TryComp<DmArenaMindSwapComponent>(tempBody, out var swap)
            && TryComp<MindComponent>(swap.OriginalMind, out var origMind)
            && origMind.OwnedEntity is { } originalBody)
            RemComp<DmArenaDormantBodyComponent>(originalBody);
    }

    private void OnDespawnPickedUp(Entity<DmArenaSweptComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        EntityUid? owner = args.Container.Owner;
        while (owner is { } current)
        {
            if (HasComp<DmArenaFighterComponent>(current))
            {
                CancelDespawn(ent);
                return;
            }

            owner = _container.TryGetContainingContainer((current, null, null), out var outer) ? outer.Owner : null;
        }
    }

    private void CancelDespawn(EntityUid uid)
    {
        if (HasComp<DmArenaSweptComponent>(uid))
        {
            RemComp<DmArenaSweptComponent>(uid);
            RemComp<TimedDespawnComponent>(uid);
        }

        if (!TryComp<ContainerManagerComponent>(uid, out var manager))
            return;

        foreach (var container in _container.GetAllContainers(uid, manager))
        {
            foreach (var contained in container.ContainedEntities.ToList())
            {
                CancelDespawn(contained);
            }
        }
    }

    private void MarkForDespawn(EntityUid uid, float lifetime, bool checkContainer = false)
    {
        if (HasComp<TimedDespawnComponent>(uid) || checkContainer && _container.IsEntityInContainer(uid))
            return;

        EnsureComp<DmArenaSweptComponent>(uid);
        EnsureComp<TimedDespawnComponent>(uid).Lifetime = lifetime;
    }

    private EntityCoordinates? GetRandomSpawnPoint(DmArenaRuleComponent rule)
    {
        var points = new List<EntityCoordinates>();

        var query = EntityQueryEnumerator<DmArenaSpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid is { } grid && rule.ArenaGrids.Contains(grid))
                points.Add(xform.Coordinates);
        }

        return points.Count > 0 ? _random.Pick(points) : null;
    }

    private void BroadcastPlayerCount()
    {
        RefreshLoadouts();
        SendPlayerCount();
    }

    public void RefreshLoadouts()
    {
        foreach (var eui in _activeEuis.Values)
        {
            eui.StateDirty();
        }
    }

    private void SendPlayerCount()
    {
        RaiseNetworkEvent(new DmArenaPlayerCountEvent(CountFighters(), IsOpen), Filter.Broadcast());
    }

    private int CountFighters()
    {
        if (CurrentRule is not { } rule)
            return 0;

        var count = 0;
        var fighters = EntityQueryEnumerator<DmArenaFighterComponent>();
        while (fighters.MoveNext(out var uid, out _))
        {
            if (!Terminating(uid) && rule.Players.Contains(GetNetEntity(uid)))
                count++;
        }

        return count;
    }

    public DmArenaKitEuiState GetLoadoutState(EntityUid ruleEntity, NetUserId userId)
    {
        if (!TryComp<DmArenaRuleComponent>(ruleEntity, out var rule))
            return new DmArenaKitEuiState(new List<DmArenaStoreOption>(), new List<DmArenaStoreSetOption>(), 0, 0, null, null, string.Empty);

        var items = rule.ShopItems
            .Select((item, index) => (item, index))
            .Where(x => !DebugDisabledItems.Contains(x.item.Gear.Id))
            .Select(x => BuildItemOption(x.item, x.index))
            .ToList();
        var sets = rule.Sets
            .Where(set => !set.Items.Any(gear => DebugDisabledItems.Contains(gear.Id)))
            .Select(set => BuildSetOption(rule, set))
            .Where(option => option.Items.Count > 0 && option.Cost <= rule.Budget)
            .ToList();

        rule.Presets.TryGetValue(userId, out var preset);
        rule.LastLoadouts.TryGetValue(userId, out var last);
        return new DmArenaKitEuiState(items, sets, CountFighters(), rule.Budget, Usable(rule, preset), Usable(rule, last), rule.Gear);
    }

    private List<int>? Usable(DmArenaRuleComponent rule, List<int>? indexes)
    {
        var usable = indexes != null && indexes.All(index =>
            index >= 0 && index < rule.ShopItems.Count && !DebugDisabledItems.Contains(rule.ShopItems[index].Gear.Id));

        return usable ? indexes : null;
    }

    private DmArenaStoreOption BuildItemOption(DmArenaStoreItem item, int index)
    {
        var gear = _prototype.Index(item.Gear);

        return new DmArenaStoreOption
        {
            Index = index,
            Name = Loc.GetString(item.Name),
            Description = item.Description is { } description ? Loc.GetString(description) : string.Empty,
            Category = Loc.GetString(item.Category),
            Group = item.Group is { } group ? Loc.GetString(group) : string.Empty,
            SpritePrototype = item.Sprite,
            GearPrototype = item.Gear,
            Cost = item.Cost,
            EquipFirst = item.EquipFirst,
            InHand = item.InHand,
            Slots = gear.Equipment.Keys.ToList(),
            Hands = gear.Inhand.Count,
            MaxCount = item.MaxCount,
            AmmoType = item.AmmoType ?? string.Empty,
            AmmoFor = item.AmmoFor ?? string.Empty,
        };
    }

    private DmArenaStoreSetOption BuildSetOption(DmArenaRuleComponent rule, DmArenaStoreSet set)
    {
        var option = new DmArenaStoreSetOption
        {
            Name = Loc.GetString(set.Name),
            Description = set.Description is { } description ? Loc.GetString(description) : string.Empty,
        };

        foreach (var gearId in set.Items)
        {
            var index = rule.ShopItems.FindIndex(x => x.Gear == gearId);
            if (index < 0)
                continue;

            option.Items.Add(index);
            option.Cost += rule.ShopItems[index].Cost;
        }

        return option;
    }

    public void SavePreset(EntityUid ruleEntity, NetUserId userId, List<int> items)
    {
        if (TryComp<DmArenaRuleComponent>(ruleEntity, out var rule) && TryGetValidItems(rule, items, out _))
            rule.Presets[userId] = new List<int>(items);
    }

    private bool TryGetValidItems(DmArenaRuleComponent rule, List<int> indexes, out List<(DmArenaStoreItem Item, int Count)> items)
    {
        items = new List<(DmArenaStoreItem, int)>();

        if (indexes.Count > rule.Budget)
            return false;

        var counts = new Dictionary<int, int>();
        foreach (var index in indexes)
        {
            if (index < 0 || index >= rule.ShopItems.Count)
                return false;

            counts[index] = counts.GetValueOrDefault(index) + 1;
        }

        var cost = 0;
        var hands = 0;
        var usedSlots = new HashSet<string>();
        var ammoTypes = new HashSet<string>();

        foreach (var (index, count) in counts)
        {
            var item = rule.ShopItems[index];
            if (DebugDisabledItems.Contains(item.Gear.Id))
                return false;

            var gear = _prototype.Index(item.Gear);

            var equipable = gear.Equipment.Count > 0 || gear.Inhand.Count > 0;
            if (count > item.MaxCount || count > 1 && equipable)
                return false;

            if (gear.Equipment.Keys.Any(slot => !usedSlots.Add(slot)))
                return false;

            if (item.AmmoType != null)
                ammoTypes.Add(item.AmmoType);

            hands += gear.Inhand.Count;
            cost += item.Cost * count;
            items.Add((item, count));
        }

        var ammoMatches = items.All(x => x.Item.AmmoFor is null or "base" || ammoTypes.Contains(x.Item.AmmoFor));
        return ammoMatches && cost <= rule.Budget && hands <= 2;
    }

    private void RefillAmmo(EntityUid killer)
    {
        var pending = new Stack<EntityUid>();
        pending.Push(killer);

        while (pending.TryPop(out var current))
        {
            if (!TryComp<ContainerManagerComponent>(current, out var manager))
                continue;

            foreach (var container in _container.GetAllContainers(current, manager))
            {
                var spentAmmo = container.ID is "gun_chamber" or "revolver-ammo";

                foreach (var contained in container.ContainedEntities)
                {
                    pending.Push(contained);

                    if (!spentAmmo)
                        RefillProviders(contained);
                }
            }
        }
    }

    private void RefillProviders(EntityUid uid)
    {
        if (TryComp<BallisticAmmoProviderComponent>(uid, out var ballistic))
            _gun.RefillBallisticAmmo((uid, ballistic));

        if (HasComp<BatteryAmmoProviderComponent>(uid))
            RefillBattery(uid);

        if (TryComp<RevolverAmmoProviderComponent>(uid, out var revolver))
            _gun.RefillRevolver((uid, revolver));
    }

    private void RefillBattery(EntityUid uid)
    {
        var getCharge = new GetChargeEvent();
        RaiseLocalEvent(uid, ref getCharge);

        var missing = getCharge.MaxCharge - getCharge.CurrentCharge;
        if (getCharge.MaxCharge <= 0 || missing <= 0)
            return;

        var change = new ChangeChargeEvent(missing);
        RaiseLocalEvent(uid, ref change);
    }
}
