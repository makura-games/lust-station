using Content.Shared.Humanoid;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;

namespace Content.Shared._Lust.LockableEquipment;

public sealed class SexEquipRestrictionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        // EquipmentContainer path: raised on the device before it is installed into a container.
        SubscribeLocalEvent<SexEquipRestrictionComponent, EquipmentContainerAttachAttemptEvent>(OnAttachAttempt);

        // Clothing path: raised on the item when it is being equipped to an inventory slot.
        SubscribeLocalEvent<SexEquipRestrictionComponent, BeingEquippedAttemptEvent>(OnBeingEquipped);
    }

    private void OnAttachAttempt(Entity<SexEquipRestrictionComponent> ent, ref EquipmentContainerAttachAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<HumanoidProfileComponent>(args.Target, out var humanoid))
            return;

        if (ent.Comp.AllowedSexes.Contains(humanoid.Sex))
            return;

        args.Cancel();
        args.Reason = "sex-equip-restriction-blocked";
    }

    private void OnBeingEquipped(Entity<SexEquipRestrictionComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<HumanoidProfileComponent>(args.EquipTarget, out var humanoid))
            return;

        if (ent.Comp.AllowedSexes.Contains(humanoid.Sex))
            return;

        args.Cancel();

        _popup.PopupClient(
            Loc.GetString("sex-equip-restriction-blocked"),
            args.EquipTarget,
            args.Equipee);
    }
}
