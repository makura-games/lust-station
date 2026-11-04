using Content.Client._Lust.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    private void UpdateLustRevolverAmmo(Entity<RevolverAmmoProviderComponent> ent, ref UpdateAmmoCounterEvent args)
    {
        if (args.Control is not RevolverCylinderStatusControl control)
            return;

        control.Update(ent.Comp.CurrentIndex, ent.Comp.Chambers);
    }

    private RevolverCylinderStatusControl CreateLustRevolverCounter()
        => new();
}
