using Content.Shared.Mech.Components;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechSystem
{
    /// <summary>
    /// Отменяет попытку выстрела, если стреляет оружие пилота, а не установленное в мехе
    /// </summary>
    [SubscribeLocalEvent]
    private void OnPilotShotAttempt(Entity<VehicleOperatorComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.Vehicle is not { } vehicle || !TryComp<MechComponent>(vehicle, out var mech))
            return;

        if (mech.EquipmentContainer.Contains(args.Used))
            return;

        args.Cancel();
    }
}
