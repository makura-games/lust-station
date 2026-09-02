using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Lust.LockableEquipment;

public sealed class LockableEquipmentSystem : EntitySystem
{
    private const int UserResolveDepthLimit = 10;

    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<LockableEquipmentComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LockableEquipmentComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<LockableEquipmentComponent, LockableEquipmentBreakDoAfterEvent>(OnBreakDoAfter);

        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    private void OnStartup(Entity<LockableEquipmentComponent> ent, ref ComponentStartup _)
    {
        RefreshIconState(ent);
    }

    private void OnInteractUsing(Entity<LockableEquipmentComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !args.Used.IsValid())
            return;

        if (TryRepair(ent, args.Used, args.User) ||
            TryStartBreakDoAfter(ent, args.Used, args.User) ||
            HasComp<KeyComponent>(args.Used) && TryUseKey(ent, args.Used, args.User))
        {
            args.Handled = true;
        }
    }

    private void OnBreakDoAfter(Entity<LockableEquipmentComponent> ent, ref LockableEquipmentBreakDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } tool)
            return;

        args.Handled = TryBreak(ent, tool, args.User);
    }

    /// <summary>
    /// Handles key interaction for the device and returns true when the interaction was processed,
    /// including blocked or rejected attempts that already displayed feedback.
    /// </summary>
    public bool TryUseKey(EntityUid device, EntityUid keyUid, EntityUid user)
    {
        if (!TryComp<KeyComponent>(keyUid, out var key))
            return false;

        if (!TryComp<LockableEquipmentComponent>(device, out var lockComp))
            return false;

        var keyName = MetaData(keyUid).EntityName;
        var name = MetaData(device).EntityName;

        if (lockComp.Broken)
        {
            _popup.PopupClient(
                Loc.GetString("lockable-equipment-is-broken", ("name", name)),
                user);
            return true;
        }

        if (key.LockId == null && lockComp.LockId == null)
        {
            if (_net.IsServer)
            {
                var id = Guid.NewGuid().ToString();
                key.LockId = id;
                lockComp.LockId = id;
                Dirty(keyUid, key);
                Dirty(device, lockComp);
            }

            _popup.PopupClient(
                Loc.GetString("lockable-equipment-paired",
                    ("key", keyName),
                    ("device", name)),
                user);

            return true;
        }

        if (key.LockId != lockComp.LockId)
        {
            _popup.PopupClient(
                Loc.GetString("lockable-equipment-wrong-key",
                    ("key", keyName),
                    ("device", name)),
                user);
            return true;
        }

        lockComp.Locked = !lockComp.Locked;

        var msg = lockComp.Locked
            ? Loc.GetString("lockable-equipment-locked", ("name", name))
            : Loc.GetString("lockable-equipment-unlocked", ("name", name));

        _popup.PopupClient(msg, user, user);

        RefreshIconState((device, lockComp));
        Dirty(device, lockComp);
        return true;
    }

    /// <summary>
    /// Handles a forced-open attempt and returns true when the interaction was processed,
    /// including blocked attempts that already displayed feedback.
    /// </summary>
    public bool TryStartBreakDoAfter(Entity<LockableEquipmentComponent> ent, EntityUid tool, EntityUid user, EntityUid? interactionTarget = null)
    {
        return TryStartBreakDoAfter((EntityUid) ent, tool, user, interactionTarget);
    }

    /// <summary>
    /// Handles a forced-open attempt and returns true when the interaction was processed,
    /// including blocked attempts that already displayed feedback.
    /// </summary>
    public bool TryStartBreakDoAfter(EntityUid device, EntityUid tool, EntityUid user, EntityUid? interactionTarget = null)
    {
        if (!user.IsValid() || Deleted(user))
            return false;

        if (!TryComp<LockableEquipmentComponent>(device, out var comp))
            return false;

        if (!CanBreakWithTool(device, tool, comp))
            return false;

        if (comp.Mode == LockableEquipmentComponent.BreakMode.None)
        {
            _popup.PopupClient(
                Loc.GetString("lockable-equipment-cannot-be-forced-opened", ("name", MetaData(device).EntityName)),
                user);
            return true;
        }

        if (comp.Broken || !comp.Locked)
            return false;

        if (IsInUser(device, user))
        {
            _popup.PopupClient(
                Loc.GetString("lockable-equipment-self-action"),
                user);
            return true;
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            comp.BreakDoAfter,
            new LockableEquipmentBreakDoAfterEvent(),
            device,
            target: interactionTarget ?? device,
            used: tool)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
            DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    /// <summary>
    /// Resolves the configured forced-open behavior and returns true when the interaction was processed,
    /// including blocked attempts that already displayed feedback.
    /// </summary>
    public bool TryBreak(EntityUid device, EntityUid tool, EntityUid user)
    {
        if (!TryComp<LockableEquipmentComponent>(device, out var comp))
            return false;

        if (!CanBreakWithTool(device, tool, comp))
            return false;

        if (comp.Broken || !comp.Locked)
            return false;

        if (IsInUser(device, user))
        {
            _popup.PopupClient(
                Loc.GetString("lockable-equipment-self-action"),
                user);
            return true;
        }

        var name = MetaData(device).EntityName;

        switch (comp.Mode)
        {
            case LockableEquipmentComponent.BreakMode.ForceOpen:
                comp.Locked = false;
                _popup.PopupClient(
                    Loc.GetString("lockable-equipment-force-open", ("name", name)),
                    user);
                break;

            case LockableEquipmentComponent.BreakMode.Breakable:
                comp.Locked = false;
                comp.Broken = true;
                _popup.PopupClient(
                    Loc.GetString("lockable-equipment-broken", ("name", name)),
                    user);
                break;

            case LockableEquipmentComponent.BreakMode.Destroyable:
                _popup.PopupClient(
                    Loc.GetString("lockable-equipment-destroyed", ("name", name)),
                    user);

                if (_timing.InPrediction)
                    return true;

                QueueDel(device);
                return true;
        }

        RefreshIconState((device, comp));
        Dirty(device, comp);
        return true;
    }

    /// <summary>
    /// Attempts to repair a broken device and returns true when the interaction was processed,
    /// including blocked attempts that already displayed feedback.
    /// </summary>
    public bool TryRepair(EntityUid device, EntityUid material, EntityUid user)
    {
        if (!TryComp<LockableEquipmentComponent>(device, out var comp))
            return false;

        if (!CanRepairWithMaterial(device, material, comp))
            return false;

        if (!TryComp<StackComponent>(material, out var stack))
            return false;

        if (!_stack.TryUse((material, stack), comp.RepairAmount))
            return false;

        comp.Broken = false;
        comp.Locked = false;

        var name = MetaData(device).EntityName;
        _popup.PopupClient(
            Loc.GetString("lockable-equipment-repaired", ("name", name)),
            user);

        RefreshIconState((device, comp));
        Dirty(device, comp);
        return true;
    }

    /// <summary>
    /// Returns true when the given tool can force the device open.
    /// </summary>
    public bool CanBreakWithTool(EntityUid device, EntityUid tool, LockableEquipmentComponent? comp = null)
    {
        if (!Resolve(device, ref comp, false))
            return false;

        return _tag.HasTag(tool, comp.RequiredToolTag);
    }

    /// <summary>
    /// Returns true when the given stack can repair the device.
    /// </summary>
    public bool CanRepairWithMaterial(EntityUid device, EntityUid material, LockableEquipmentComponent? comp = null)
    {
        if (!Resolve(device, ref comp, false))
            return false;

        if (!comp.Broken || comp.RepairMaterial == null || comp.RepairAmount <= 0)
            return false;

        if (!TryComp<StackComponent>(material, out var stack))
            return false;

        return stack.StackTypeId == comp.RepairMaterial && stack.Count >= comp.RepairAmount;
    }

    public void RefreshIconState(Entity<LockableEquipmentComponent> ent)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        var state = ent.Comp.Locked && !ent.Comp.Broken ? "icon_locked" : "icon";
        _appearance.SetData(ent, EquipmentVisuals.IconState, state, appearance);
    }

    /// <summary>
    /// Walks the transform parent chain to check if <paramref name="device"/> is contained inside
    /// <paramref name="user"/>. The depth limit guards against cycles or abnormally deep nesting
    /// in the Transform hierarchy (e.g. map→grid→tile chains).
    /// </summary>
    private bool IsInUser(EntityUid device, EntityUid user)
    {
        if (!_xformQuery.TryComp(device, out var deviceXform))
            return false;

        var current = deviceXform.ParentUid;
        var depth = 0;

        while (current.IsValid() && depth < UserResolveDepthLimit)
        {
            depth++;
            if (current == user)
                return true;

            if (!_xformQuery.TryComp(current, out var xform))
                break;

            current = xform.ParentUid;
        }

        return false;
    }

}
