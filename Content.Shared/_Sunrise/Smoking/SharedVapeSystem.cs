using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Smoking;

/// <summary>
/// Общая система электронных испарителей (вейпов) с клиентским предсказанием затяжек и визуалом картриджей.
/// </summary>
public abstract partial class SharedVapeSystem : EntitySystem
{
    [Dependency] protected ItemSlotsSystem ItemSlots = default!;
    [Dependency] protected SharedSolutionContainerSystem Solution = default!;
    [Dependency] protected SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected IngestionSystem Ingestion = default!;
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] protected IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VapeComponent, MapInitEvent>(OnVapeMapInit);
        SubscribeLocalEvent<VapeComponent, UseInHandEvent>(OnVapeUseInHand);
        SubscribeLocalEvent<VapeComponent, AfterInteractEvent>(OnVapeAfterInteract);
        SubscribeLocalEvent<VapeComponent, GetItemActionsEvent>(OnVapeGetActions);
        SubscribeLocalEvent<VapeComponent, BlowSmokeRingActionEvent>(OnVapeBlowSmokeRing);
        SubscribeLocalEvent<VapeComponent, EntInsertedIntoContainerMessage>(OnVapeInserted);
        SubscribeLocalEvent<VapeComponent, EntRemovedFromContainerMessage>(OnVapeRemoved);

        SubscribeLocalEvent<VapeCartridgeComponent, MapInitEvent>(OnCartridgeMapInit);
        SubscribeLocalEvent<VapeCartridgeComponent, SolutionChangedEvent>(OnCartridgeSolutionChanged);
    }

    private void OnVapeMapInit(Entity<VapeComponent> entity, ref MapInitEvent args)
    {
        UpdateVapeAppearance(entity);
    }

    private void OnCartridgeMapInit(Entity<VapeCartridgeComponent> entity, ref MapInitEvent args)
    {
        UpdateCartridgeAppearance(entity);
    }

    private void OnCartridgeSolutionChanged(Entity<VapeCartridgeComponent> entity, ref SolutionChangedEvent args)
    {
        UpdateCartridgeAppearance(entity);
    }

    public void UpdateCartridgeAppearance(Entity<VapeCartridgeComponent> entity)
    {
        var flavor = "empty";

        if (Solution.TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution) &&
            solution.Volume > FixedPoint2.Zero)
        {
            var maxAmount = FixedPoint2.Zero;
            foreach (var reagent in solution.Contents)
            {
                if (entity.Comp.FlavorVisuals.TryGetValue(reagent.Reagent.Prototype, out var flavorKey) &&
                    reagent.Quantity > maxAmount)
                {
                    maxAmount = reagent.Quantity;
                    flavor = flavorKey;
                }
            }
        }

        entity.Comp.CurrentFlavor = flavor;
        Appearance.SetData(entity.Owner, VapeVisuals.CartridgeFlavor, flavor);

        if (Container.TryGetContainingContainer((entity.Owner, null, null), out var container) &&
            TryComp<VapeComponent>(container.Owner, out var vape))
        {
            UpdateVapeAppearance((container.Owner, vape));
        }
    }

    private void OnVapeInserted(Entity<VapeComponent> entity, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == entity.Comp.CartridgeSlot)
        {
            UpdateVapeAppearance(entity);
        }
    }

    private void OnVapeRemoved(Entity<VapeComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == entity.Comp.CartridgeSlot)
        {
            UpdateVapeAppearance(entity);
        }
    }

    public void UpdateVapeAppearance(Entity<VapeComponent> entity)
    {
        var hasCartridge = false;
        var flavor = "no-cartridge";

        if (TryComp<ItemSlotsComponent>(entity.Owner, out var itemSlots) &&
            ItemSlots.TryGetSlot(entity.Owner, entity.Comp.CartridgeSlot, out var slot, itemSlots))
        {
            hasCartridge = slot.Item != null;
            if (slot.Item != null && TryComp<VapeCartridgeComponent>(slot.Item.Value, out var cartridge))
            {
                flavor = cartridge.CurrentFlavor;
            }
        }

        Appearance.SetData(entity.Owner, VapeVisuals.HasCartridge, hasCartridge);
        Appearance.SetData(entity.Owner, VapeVisuals.CartridgeFlavor, flavor);
    }

    private void OnVapeUseInHand(Entity<VapeComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryStartVape(entity, args.User, args.User))
            args.Handled = true;
    }

    private void OnVapeAfterInteract(Entity<VapeComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryStartVape(entity, args.User, args.Target.Value))
            args.Handled = true;
    }

    public bool CanVape(
        Entity<VapeComponent> entity,
        EntityUid user,
        EntityUid target,
        out EntityUid? cartridgeUid,
        out Solution? solution,
        bool checkCooldown = true,
        bool quiet = false)
    {
        cartridgeUid = null;
        solution = null;

        if (!TryComp<ItemSlotsComponent>(entity.Owner, out var itemSlots) ||
            !ItemSlots.TryGetSlot(entity.Owner, entity.Comp.CartridgeSlot, out var slot, itemSlots) ||
            slot.Item == null)
        {
            if (!quiet)
                Popup.PopupClient(Loc.GetString("vape-component-no-cartridge"), user, user);
            return false;
        }

        cartridgeUid = slot.Item.Value;

        if (!TryComp<VapeCartridgeComponent>(cartridgeUid.Value, out var cartridge) ||
            !Solution.TryGetSolution(cartridgeUid.Value, cartridge.Solution, out _, out solution) ||
            solution.Volume <= FixedPoint2.Zero)
        {
            if (!quiet)
                Popup.PopupClient(Loc.GetString("vape-component-cartridge-empty"), user, user);
            return false;
        }

        if (!HasComp<BloodstreamComponent>(target))
            return false;

        if (!Ingestion.HasMouthAvailable(user, target))
        {
            if (!quiet)
                Popup.PopupClient(Loc.GetString("vape-component-mouth-blocked"), user, user);
            return false;
        }

        if (checkCooldown && Timing.CurTime < entity.Comp.LastPuffTime + entity.Comp.Cooldown)
            return false;

        return true;
    }

    public bool TryStartVape(Entity<VapeComponent> entity, EntityUid user, EntityUid target)
    {
        if (!CanVape(entity, user, target, out _, out var solution))
            return false;

        var forced = target != user;
        var delay = forced ? entity.Comp.Delay : entity.Comp.UserDelay;

        entity.Comp.LastPuffTime = Timing.CurTime;
        Dirty(entity);

        if (entity.Comp.PuffSound != null)
        {
            Audio.PlayPredicted(entity.Comp.PuffSound, entity.Owner, user);
        }

        if (forced)
        {
            var targetName = Identity.Entity(target, EntityManager);
            var userName = Identity.Entity(user, EntityManager);
            Popup.PopupPredicted(Loc.GetString("vape-component-try-use-vape-forced", ("user", userName)),
                Loc.GetString("vape-component-try-use-vape-forced-user", ("target", targetName)),
                target, user);
        }
        else
        {
            Popup.PopupPredicted(Loc.GetString("vape-component-try-use-vape"), target, user);
        }

        var vapeDoAfterEvent = new VapeDoAfterEvent(solution!, forced);
        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, vapeDoAfterEvent, entity.Owner, target: target, used: entity.Owner)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = true,
        };

        return DoAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnVapeGetActions(Entity<VapeComponent> entity, ref GetItemActionsEvent args)
    {
        args.AddAction(ref entity.Comp.SmokeRingActionEntity, entity.Comp.SmokeRingAction);
    }

    private void OnVapeBlowSmokeRing(Entity<VapeComponent> entity, ref BlowSmokeRingActionEvent args)
    {
        if (args.Handled)
            return;

        if (TryStartBlowVapeSmokeRing(entity, args.Performer, args.Target))
            args.Handled = true;
    }

    public bool TryStartBlowVapeSmokeRing(Entity<VapeComponent> entity, EntityUid user, EntityCoordinates target)
    {
        if (!CanVape(entity, user, user, out _, out _))
            return false;

        entity.Comp.LastPuffTime = Timing.CurTime;
        Dirty(entity);

        if (entity.Comp.PuffSound != null)
        {
            Audio.PlayPredicted(entity.Comp.PuffSound, entity.Owner, user);
        }

        Popup.PopupPredicted(Loc.GetString("smoking-puff-try"), user, user);

        var doAfterArgs = new DoAfterArgs(EntityManager, user, entity.Comp.UserDelay, new SmokeRingDoAfterEvent(GetNetCoordinates(target)), entity.Owner, target: user, used: entity.Owner)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = true,
        };

        return DoAfter.TryStartDoAfter(doAfterArgs);
    }
}
