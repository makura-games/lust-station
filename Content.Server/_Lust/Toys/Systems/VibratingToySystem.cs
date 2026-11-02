using Content.Server.Speech.Components;
using Content.Server._Lust.Toys.Components;
using Content.Shared.Inventory.Events;
using Content.Shared._Lust.Toys.Systems;
using Content.Shared._Lust.Toys.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Jittering;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffectNew;

namespace Content.Server._Lust.Toys.Systems;

public sealed partial class VibratingToySystem : SharedToySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private StutteringSystem _stuttering = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    protected override void OnGotEquipped(EntityUid uid, VibratingToyComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);
        if (!component.IsEquipped || !component.Enabled)
            return;
        EnsureComp<VibratingComponent>(args.EquipTarget).Toy = uid;

        EnsureComp<StutteringAccentComponent>(args.EquipTarget);

        _stuttering.DoStutter(args.EquipTarget, TimeSpan.FromSeconds(30), true);

        if (!TryComp<MovementSpeedModifierComponent>(args.EquipTarget, out var modifierComponent))
            return;

        if (component.BaseWalkSpeed != null || component.BaseSprintSpeed != null || component.BaseAcceleration != null)
            return;

        component.BaseSprintSpeed = modifierComponent?.BaseSprintSpeed;
        component.BaseWalkSpeed = modifierComponent?.BaseWalkSpeed;
        component.BaseAcceleration = modifierComponent?.Acceleration;

        _movementSpeedModifier.ChangeBaseSpeed(
            args.EquipTarget,
            component.TargetWalkSpeed,
            component.TargetSprintSpeed,
            component.TargetAcceleration);
    }

    protected override void OnGotUnequipped(EntityUid uid, VibratingToyComponent component, GotUnequippedEvent args)
    {
        base.OnGotUnequipped(uid, component, args);
        if (component.IsEquipped)
            return;

        RemComp<VibratingComponent>(args.EquipTarget);
        RemComp<StutteringAccentComponent>(args.EquipTarget);
        RemComp<JitteringComponent>(args.EquipTarget);
        RemComp<StutteringAccentComponent>(args.EquipTarget);

        if (!_statusEffects.TryRemoveStatusEffect(args.EquipTarget, "StatusEffectSlurred"))
            return;

        if (!TryComp<MovementSpeedModifierComponent>(args.EquipTarget, out var modifierComponent))
            return;

        if (component.BaseWalkSpeed == null || component.BaseSprintSpeed == null || component.BaseAcceleration == null)
            return;

        _movementSpeedModifier.ChangeBaseSpeed(args.EquipTarget, component.BaseWalkSpeed.Value, component.BaseSprintSpeed.Value, component.BaseAcceleration.Value);
        component.BaseWalkSpeed = null;
        component.BaseSprintSpeed = null;
        component.BaseAcceleration = null;
    }
}
