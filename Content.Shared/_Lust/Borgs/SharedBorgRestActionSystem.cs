using Content.Shared._Lust.Borgs.Components;
using Content.Shared.Actions;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Rotation;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Player;

namespace Content.Shared._Lust.Borgs;

public sealed partial class SharedBorgRestActionSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgRestActionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BorgRestActionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BorgRestActionComponent, BorgToggleRestActionEvent>(OnToggleRest);
        SubscribeLocalEvent<BorgRestingComponent, ComponentStartup>(OnRestingStartup);
        SubscribeLocalEvent<BorgRestingComponent, ComponentRemove>(OnRestingRemove);
        SubscribeLocalEvent<KnockedDownComponent, ComponentRemove>(OnKnockedDownRemove);
        SubscribeLocalEvent<BorgRestingComponent, RefreshMovementSpeedModifiersEvent>(OnRestingSpeed);
        SubscribeLocalEvent<BorgRestingComponent, RefreshWeightlessModifiersEvent>(OnRestingWeightless);
        SubscribeLocalEvent<BorgRestingComponent, RefreshChargeRateEvent>(OnRestingChargeRate);
    }

    private void OnStartup(Entity<BorgRestActionComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction);
        UpdateActionState(ent);
    }

    private void OnShutdown(Entity<BorgRestActionComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ToggleActionEntity);
    }

    private void OnToggleRest(Entity<BorgRestActionComponent> ent, ref BorgToggleRestActionEvent args)
    {
        if (args.Handled)
            return;

        if (TryToggleRest(ent.AsNullable()))
            args.Handled = true;
    }

    private void OnRestingStartup(Entity<BorgRestingComponent> ent, ref ComponentStartup args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
        _movementSpeed.RefreshWeightlessModifiers(ent);
        RefreshBatteryChargeRate(ent);

        if (TryComp<BorgRestActionComponent>(ent, out var borgRest))
            UpdateActionState((ent.Owner, borgRest));
    }

    private void OnRestingRemove(Entity<BorgRestingComponent> ent, ref ComponentRemove args)
    {
        ClearRestingVisual(ent.Owner);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
        _movementSpeed.RefreshWeightlessModifiers(ent);
        RefreshBatteryChargeRate(ent);

        if (TryComp<BorgRestActionComponent>(ent, out var borgRest))
            UpdateActionState((ent.Owner, borgRest), resting: false);
    }

    private void OnKnockedDownRemove(Entity<KnockedDownComponent> ent, ref ComponentRemove args)
    {
        if (HasComp<BorgRestingComponent>(ent))
            SetRestingVisualRotation(ent.Owner, resting: true);
    }

    private void OnRestingSpeed(Entity<BorgRestingComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(0f);
    }

    private void OnRestingWeightless(Entity<BorgRestingComponent> ent, ref RefreshWeightlessModifiersEvent args)
    {
        args.WeightlessModifier = 0f;
    }

    private void OnRestingChargeRate(Entity<BorgRestingComponent> ent, ref RefreshChargeRateEvent args)
    {
        // Lust edit - компенсируем обычный расход борга и добавляем 10% ёмкости за две минуты.
        if (TryComp<PowerCellDrawComponent>(ent, out var draw) && draw.Enabled)
            args.NewChargeRate += draw.DrawRate;

        args.NewChargeRate += args.MaxCharge / 1200f;
    }

    private void RefreshBatteryChargeRate(EntityUid uid)
    {
        if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            _battery.RefreshChargeRate(battery.Value.AsNullable());
    }

    private void UpdateActionState(Entity<BorgRestActionComponent> ent)
    {
        UpdateActionState(ent, HasComp<BorgRestingComponent>(ent));
    }

    private void UpdateActionState(Entity<BorgRestActionComponent> ent, bool resting)
    {
        _actions.SetToggled(ent.Comp.ToggleActionEntity, resting);
    }

    /// <summary>
    /// Пытается переключить состояние отдыха борга.
    /// </summary>
    public bool TryToggleRest(Entity<BorgRestActionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!CanToggleRest(ent))
            return false;

        Entity<BorgRestActionComponent> borgRest = (ent.Owner, ent.Comp);

        if (HasComp<BorgRestingComponent>(ent))
            return DoStandUp(borgRest);

        return DoRest(borgRest);
    }

    /// <summary>
    /// Проверяет, может ли борг переключить состояние отдыха.
    /// </summary>
    public bool CanToggleRest(Entity<BorgRestActionComponent?> ent, bool quiet = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (HasComp<BorgRestingComponent>(ent))
            return !HasComp<KnockedDownComponent>(ent);

        if (!HasComp<BorgChassisComponent>(ent))
            return false;

        if (!HasComp<ActorComponent>(ent))
            return false;

        return _mobState.IsAlive(ent) && !HasComp<KnockedDownComponent>(ent);
    }

    private bool DoRest(Entity<BorgRestActionComponent> ent)
    {
        EnsureComp<BorgRestingComponent>(ent);
        SetRestingVisual(ent, true);
        return true;
    }

    private bool DoStandUp(Entity<BorgRestActionComponent> ent)
    {
        if (!RemComp<BorgRestingComponent>(ent))
            return false;

        RefreshBatteryChargeRate(ent);
        return true;
    }

    private void SetRestingVisualRotation(EntityUid uid, bool resting)
    {
        _appearance.SetData(uid, RotationVisuals.RotationState, resting ? RotationState.Horizontal : RotationState.Vertical);
    }

    private void SetRestingVisual(EntityUid uid, bool resting)
    {
        _appearance.SetData(uid, BorgRestVisuals.Resting, resting);
        SetRestingVisualRotation(uid, resting);
    }

    private void ClearRestingVisual(EntityUid uid)
    {
        SetRestingVisual(uid, false);
    }
}
