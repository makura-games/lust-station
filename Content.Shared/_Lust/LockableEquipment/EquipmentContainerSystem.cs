using Content.Shared.Containers;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared._Lust.LockableEquipment;

/// <summary>
/// Handles installing and removing lockable devices from an entity-owned container.
/// </summary>
public sealed class EquipmentContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly LockableEquipmentSystem _lockable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EquipmentContainerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EquipmentContainerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EquipmentContainerComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<EquipmentContainerComponent, EquipmentDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<EquipmentContainerComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<EquipmentContainerComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
    }

    private void OnInit(Entity<EquipmentContainerComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
    }

    private void OnInteractUsing(Entity<EquipmentContainerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        BaseContainer? container = null;
        EntityUid? installedDevice = null;

        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var existingContainer))
        {
            container = existingContainer;
            installedDevice = FindDevice(existingContainer);
        }

        if (installedDevice != null)
        {
            if (!TryComp(installedDevice.Value, out LockableEquipmentComponent? installedComp))
                return;

            if (!IsCoveredByClothing(ent) &&
                TryInteractWithInstalledDevice(ent, (installedDevice.Value, installedComp), args.User, args.Used, out var handled))
            {
                args.Handled = handled;
                return;
            }
        }

        if (!TryComp(args.Used, out LockableEquipmentComponent? device))
            return;

        args.Handled = TryAttachDevice(ent, args.User, (args.Used, device), container);
    }

    private void OnGetVerbs(Entity<EquipmentContainerComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (TryGetEquipment(ent) is not {} device)
            return;

        if (IsCoveredByClothing(ent))
            return;

        if (!TryComp(device, out LockableEquipmentComponent? comp))
            return;

        var user = args.User;
        var name = MetaData(device).EntityName;
        var addedKeyVerb = false;
        var addedBreakVerb = false;

        foreach (var hand in _hands.EnumerateHands(args.User))
        {
            if (!_hands.TryGetHeldItem(args.User, hand, out var held))
                continue;

            if (!addedKeyVerb && !comp.Broken && HasComp<KeyComponent>(held.Value))
            {
                addedKeyVerb = true;

                args.Verbs.Add(new InteractionVerb
                {
                    Text = comp.Locked
                        ? Loc.GetString("lockable-equipment-verb-unlock", ("name", name))
                        : Loc.GetString("lockable-equipment-verb-lock", ("name", name)),
                    Act = () => TryUseHeldKey(ent, user)
                });
            }

            if (!addedBreakVerb && comp.Locked && _lockable.CanBreakWithTool(device, held.Value, comp))
            {
                var breakText = GetBreakVerbText(name, comp.Mode);
                if (breakText != null)
                {
                    addedBreakVerb = true;
                    args.Verbs.Add(new InteractionVerb
                    {
                        Text = breakText,
                        Act = () => TryBreakWithHeldTool(ent, user)
                    });
                }
            }
        }

        if (!comp.Locked)
        {
            args.Verbs.Add(new InteractionVerb
            {
                Text = Loc.GetString("lockable-equipment-verb-remove", ("name", name)),
                Act = () => TryRemove(ent, user, ent.Comp)
            });
        }
    }

    private void OnDoAfter(Entity<EquipmentContainerComponent> ent, ref EquipmentDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        switch (args.Action)
        {
            case EquipmentActionType.Attach:
            {
                if (args.Used is not { } used)
                    return;

                if (!TryComp(used, out LockableEquipmentComponent? device))
                    return;

                if (FindDevice(container) != null)
                    return;

                if (IsCoveredByClothing(ent))
                    return;

                var attempt = new EquipmentContainerAttachAttemptEvent(ent, args.User);
                RaiseLocalEvent(used, attempt);
                if (attempt.Cancelled)
                {
                    if (attempt.Reason != null)
                        _popup.PopupClient(Loc.GetString(attempt.Reason), args.User);
                    return;
                }

                if (!_container.Insert(used, container))
                    return;

                PopupEquipmentEquipped(used, args.User);
                break;
            }

            case EquipmentActionType.Detach:
            {
                var device = FindDevice(container);

                if (device == null)
                {
                    ResetAppearance(ent);
                    break;
                }

                if (!CanRemove(container, args.User))
                    return;

                if (!_container.Remove(device.Value, container))
                    return;

                if (!_hands.TryPickup(args.User, device.Value, checkActionBlocker: false))
                    _transform.DropNextTo(device.Value, args.User);

                PopupEquipmentRemoved(device.Value, args.User);
                break;
            }
        }

        args.Handled = true;
    }

    private void OnContainerInserted(Entity<EquipmentContainerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (TryComp(args.Entity, out LockableEquipmentComponent? device))
            UpdateAppearance(ent, device);

        var insertedEv = new EquipmentContainerChangedEvent();
        RaiseLocalEvent(ent, ref insertedEv);
    }

    private void OnContainerRemoved(Entity<EquipmentContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (TryComp(args.Entity, out LockableEquipmentComponent? device))
            ResetAppearance(ent, device);

        var removedEv = new EquipmentContainerChangedEvent();
        RaiseLocalEvent(ent, ref removedEv);
    }

    /// <summary>
    /// Attempts to remove the currently installed device from the target.
    /// </summary>
    public bool TryRemove(EntityUid target, EntityUid? user, EquipmentContainerComponent? comp = null)
    {
        if (user == null || Deleted(user.Value))
            return false;

        if (!Resolve(target, ref comp))
            return false;

        if (IsCoveredByClothing(target))
        {
            _popup.PopupClient(Loc.GetString("lockable-equipment-blocked"), user.Value);
            return false;
        }

        if (!_container.TryGetContainer(target, comp.ContainerId, out var container) || !CanRemove(container, user.Value))
            return false;

        var installedDevice = FindDevice(container);
        if (installedDevice is not { } deviceUid)
            return false;

        if (!TryComp(deviceUid, out LockableEquipmentComponent? installedComp))
            return false;

        var doAfter = new DoAfterArgs(
            EntityManager,
            user.Value,
            installedComp.DetachDoAfter,
            new EquipmentDoAfterEvent(EquipmentActionType.Detach),
            target,
            target: target)
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
    /// Attempts to start the attach flow for a device held by the user.
    /// </summary>
    public bool TryAttachDevice(
        Entity<EquipmentContainerComponent> ent,
        EntityUid? user,
        Entity<LockableEquipmentComponent> device,
        BaseContainer? container = null)
    {
        if (user == null || Deleted(user.Value))
            return false;

        if (container == null)
        {
            if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var got))
                return false;
            container = got;
        }

        if (IsCoveredByClothing(ent))
        {
            _popup.PopupClient(
                Loc.GetString("lockable-equipment-blocked"),
                user.Value);
            return false;
        }

        if (FindDevice(container) != null)
        {
            _popup.PopupClient(
                Loc.GetString("lockable-equipment-already"),
                user.Value);
            return false;
        }

        var attempt = new EquipmentContainerAttachAttemptEvent(ent, user.Value);
        RaiseLocalEvent(device, attempt);
        if (attempt.Cancelled)
        {
            if (attempt.Reason != null)
                _popup.PopupClient(Loc.GetString(attempt.Reason), user.Value);
            return false;
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            user.Value,
            device.Comp.AttachDoAfter,
            new EquipmentDoAfterEvent(EquipmentActionType.Attach),
            ent,
            target: ent,
            used: device)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
            DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    private void UpdateAppearance(EntityUid uid, LockableEquipmentComponent device)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        _appearance.TryGetData(uid, EquipmentVisuals.VisualData, out EquipmentVisualData? prev, appearance);
        if (prev != null)
            _appearance.SetData(uid, EquipmentVisuals.PreviousLayer, prev, appearance);

        var visualData = CreateVisualData(device, true);
        _appearance.SetData(uid, EquipmentVisuals.VisualData, visualData, appearance);
    }

    private void ResetAppearance(EntityUid uid, LockableEquipmentComponent? previousDevice = null)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        EquipmentVisualData? visualData = null;

        if (previousDevice != null)
        {
            visualData = CreateVisualData(previousDevice, false);
        }
        else
        {
            _appearance.TryGetData(uid, EquipmentVisuals.VisualData, out visualData, appearance);

            if (visualData != null)
                visualData = new EquipmentVisualData(false, visualData.Layer, visualData.RsiPath, visualData.State);
        }

        if (visualData != null)
        {
            _appearance.TryGetData(uid, EquipmentVisuals.VisualData, out EquipmentVisualData? prev, appearance);
            if (prev != null)
                _appearance.SetData(uid, EquipmentVisuals.PreviousLayer, prev, appearance);

            _appearance.SetData(uid, EquipmentVisuals.VisualData, visualData, appearance);
        }
    }

    private EntityUid? FindDevice(BaseContainer container)
    {
        foreach (var ent in container.ContainedEntities)
        {
            if (HasComp<LockableEquipmentComponent>(ent))
                return ent;
        }

        return null;
    }

    private EntityUid? TryGetEquipment(Entity<EquipmentContainerComponent> ent)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return null;

        return FindDevice(container);
    }

    private static readonly SlotFlags CoverFlags =
        SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING | SlotFlags.PANTS;

    private bool IsCoveredByClothing(EntityUid target)
    {
        if (!TryComp(target, out InventoryComponent? inv))
            return false;

        foreach (var slot in inv.Slots)
        {
            if ((slot.SlotFlags & CoverFlags) == 0)
                continue;

            if (_inventory.TryGetSlotEntity(target, slot.Name, out _))
                return true;
        }

        return false;
    }

    private bool TryUseHeldKey(Entity<EquipmentContainerComponent> ent, EntityUid user)
    {
        if (TryGetEquipment(ent) is not {} device)
            return false;

        foreach (var hand in _hands.EnumerateHands(user))
        {
            if (!_hands.TryGetHeldItem(user, hand, out var held))
                continue;

            if (!HasComp<KeyComponent>(held.Value))
                continue;

            return _lockable.TryUseKey(device, held.Value, user);
        }

        return false;
    }

    private bool TryBreakWithHeldTool(Entity<EquipmentContainerComponent> ent, EntityUid user)
    {
        if (TryGetEquipment(ent) is not {} device)
            return false;

        if (!TryComp(device, out LockableEquipmentComponent? comp))
            return false;

        foreach (var hand in _hands.EnumerateHands(user))
        {
            if (!_hands.TryGetHeldItem(user, hand, out var held))
                continue;

            if (!_lockable.CanBreakWithTool(device, held.Value, comp))
                continue;

            return _lockable.TryStartBreakDoAfter(device, held.Value, user, ent);
        }

        return false;
    }

    private bool TryInteractWithInstalledDevice(
        Entity<EquipmentContainerComponent> ent,
        Entity<LockableEquipmentComponent> device,
        EntityUid user,
        EntityUid used,
        out bool handled)
    {
        handled = false;

        if (_lockable.CanRepairWithMaterial(device, used, device.Comp))
        {
            handled = _lockable.TryRepair(device, used, user);
            return true;
        }

        if (HasComp<KeyComponent>(used))
        {
            handled = _lockable.TryUseKey(device, used, user);
            return true;
        }

        if (_lockable.CanBreakWithTool(device, used, device.Comp))
        {
            handled = _lockable.TryStartBreakDoAfter(device, used, user, ent);
            return true;
        }

        foreach (var hand in _hands.EnumerateHands(user))
        {
            if (!_hands.TryGetHeldItem(user, hand, out var held))
                continue;

            if (held.Value == used)
                continue;

            if (_lockable.CanRepairWithMaterial(device, held.Value, device.Comp))
            {
                handled = _lockable.TryRepair(device, held.Value, user);
                return true;
            }

            if (HasComp<KeyComponent>(held.Value))
            {
                handled = _lockable.TryUseKey(device, held.Value, user);
                return true;
            }

            if (_lockable.CanBreakWithTool(device, held.Value, device.Comp))
            {
                handled = _lockable.TryStartBreakDoAfter(device, held.Value, user, ent);
                return true;
            }
        }

        return false;
    }

    private string? GetBreakVerbText(string name, LockableEquipmentComponent.BreakMode mode)
    {
        return mode switch
        {
            LockableEquipmentComponent.BreakMode.ForceOpen =>
                Loc.GetString("lockable-equipment-verb-force-open", ("name", name)),

            LockableEquipmentComponent.BreakMode.Breakable =>
                Loc.GetString("lockable-equipment-verb-break", ("name", name)),

            LockableEquipmentComponent.BreakMode.Destroyable =>
                Loc.GetString("lockable-equipment-verb-destroy", ("name", name)),

            _ => null
        };
    }

    private void PopupEquipmentEquipped(EntityUid device, EntityUid user)
    {
        var name = MetaData(device).EntityName;
        _popup.PopupClient(
            Loc.GetString("lockable-equipment-equipped", ("name", name)),
            user);
    }

    private void PopupEquipmentRemoved(EntityUid device, EntityUid user)
    {
        var name = MetaData(device).EntityName;
        _popup.PopupClient(
            Loc.GetString("lockable-equipment-removed", ("name", name)),
            user);
    }

    private bool CanRemove(BaseContainer container, EntityUid user, bool quiet = false)
    {
        var device = FindDevice(container);
        if (device == null)
            return false;

        if (!TryComp(device.Value, out LockableEquipmentComponent? dev))
            return false;

        if (dev.Locked)
        {
            if (!quiet)
            {
                var name = MetaData(device.Value).EntityName;
                _popup.PopupClient(
                    Loc.GetString("lockable-equipment-locked", ("name", name)),
                    user);
            }

            return false;
        }

        return true;
    }

    private static EquipmentVisualData CreateVisualData(LockableEquipmentComponent device, bool visible)
    {
        return new EquipmentVisualData(
            visible,
            device.Layer,
            device.RsiPath,
            device.SpriteState);
    }
}
