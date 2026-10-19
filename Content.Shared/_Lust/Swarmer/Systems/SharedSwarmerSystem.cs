using Content.Shared._Lust.Swarmer.Components;
using Content.Shared.Alert;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Materials;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared._Lust.Swarmer.Systems;

/// <summary>
/// Shared swarmer logic: resources, integrating items and the predicted abilities.
/// Anything that needs atmospherics or authoritative spawning lives in the server system.
/// </summary>
public abstract partial class SharedSwarmerSystem : EntitySystem
{
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] protected SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected SharedMapSystem Map = default!;
    [Dependency] protected SharedPointLightSystem PointLight = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
    [Dependency] protected TurfSystem Turf = default!;
    [Dependency] protected EntityWhitelistSystem Whitelist = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedStackSystem _stack = default!;

    /// <summary>
    /// Containers holding chemical solutions are internal bookkeeping, their contents must never be dropped.
    /// </summary>
    private const string SolutionContainerPrefix = "solution@";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwarmerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SwarmerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SwarmerComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<SwarmerComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);

        InitializeAbilities();
        InitializeCombat();
    }

    private void OnMapInit(Entity<SwarmerComponent> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent.Owner, ent.Comp.ResourceAlert);
        OnSwarmerMapInit(ent);
    }

    private void OnShutdown(Entity<SwarmerComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent.Owner, ent.Comp.ResourceAlert);
    }

    private void OnEmpPulse(Entity<SwarmerComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        _damageable.TryChangeDamage(ent.Owner, ent.Comp.EmpDamage, ignoreResistances: true);
    }

    /// <summary>
    /// Swarmers have no hands: clicking an item consumes it, clicking anything else (except mobs) takes it apart.
    /// </summary>
    private void OnBeforeInteractHand(Entity<SwarmerComponent> ent, ref BeforeInteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<ItemComponent>(args.Target))
        {
            args.Handled = true;
            TryIntegrate(ent, args.Target);
            return;
        }

        if (HasComp<MobStateComponent>(args.Target))
            return;

        args.Handled = true;
        TryStartDisintegrate(ent, args.Target);
    }

    /// <summary>
    /// Consumes an item made of steel, glass or plasteel, converting it into resources.
    /// Stacks are consumed one piece at a time.
    /// </summary>
    public bool TryIntegrate(Entity<SwarmerComponent> ent, EntityUid target)
    {
        if (!CanIntegrate(ent, target))
            return false;

        ent.Comp.Resources += ent.Comp.IntegrateGain;
        Dirty(ent);

        PredictedSpawnAtPosition(ent.Comp.IntegrateEffect, TransformSystem.GetMoverCoordinates(target));

        if (TryComp<StackComponent>(target, out var stack) && stack.Count > 1)
            _stack.TryUse((target, stack), 1);
        else
        {
            // Предмет с содержимым (например, пачка сигарет) не уносит его с собой.
            // Выбрасывает только сервер: при отклонённом предсказании вещи не мелькнут на полу у клиента.
            if (_net.IsServer)
                DropContents(target);
            PredictedQueueDel(target);
        }

        return true;
    }

    public bool CanIntegrate(Entity<SwarmerComponent> ent, EntityUid target, bool quiet = false)
    {
        if (!IsIntegrable(ent, target))
        {
            if (!quiet)
                Popup.PopupClient(Loc.GetString("swarmer-integrate-incompatible", ("target", target)), ent, ent);
            return false;
        }

        if (ent.Comp.Resources + ent.Comp.IntegrateGain > ent.Comp.MaxResources)
        {
            if (!quiet)
                Popup.PopupClient(Loc.GetString("swarmer-integrate-full"), ent, ent);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Starts taking apart a structure or machine. Needs atmospherics, so only the server implements it.
    /// </summary>
    /// <returns>True if the swarmer started working on the target.</returns>
    public virtual bool TryStartDisintegrate(Entity<SwarmerComponent> ent, EntityUid target)
    {
        return false;
    }

    /// <summary>
    /// Called once a swarmer is map-initialized, for server-only setup.
    /// </summary>
    protected virtual void OnSwarmerMapInit(Entity<SwarmerComponent> ent)
    {
    }

    /// <summary>
    /// Called after a swarmer lands a melee hit, for server-only reactions.
    /// </summary>
    protected virtual void OnSwarmerHit(Entity<SwarmerComponent> ent)
    {
    }

    /// <summary>
    /// Whether the item is made of a material swarmers can recycle (or is explicitly allowed) and isn't blacklisted (guns).
    /// </summary>
    private bool IsIntegrable(Entity<SwarmerComponent> ent, EntityUid target)
    {
        if (Whitelist.IsWhitelistPass(ent.Comp.IntegrateBlacklist, target))
            return false;

        // Осколки и прочий мусор без состава материалов разрешены отдельным списком.
        if (Whitelist.IsWhitelistPass(ent.Comp.IntegrateWhitelist, target))
            return true;

        if (!TryComp<PhysicalCompositionComponent>(target, out var composition))
            return false;

        foreach (var material in ent.Comp.IntegrateMaterials)
        {
            if (composition.MaterialComposition.GetValueOrDefault(material) > 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Drops everything stored inside (locker contents, machine boards and parts) instead of deleting it.
    /// </summary>
    protected void DropContents(EntityUid uid)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var manager))
            return;

        foreach (var container in Container.GetAllContainers(uid, manager))
        {
            var id = container.ID;
            if (id.StartsWith(SolutionContainerPrefix))
                continue;

            Container.EmptyContainer(container, force: true);
        }
    }
}
