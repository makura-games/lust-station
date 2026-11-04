using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeRevolver()
    {
        base.InitializeRevolver();
        SubscribeLocalEvent<RevolverAmmoProviderComponent, AmmoCounterControlEvent>(OnRevolverCounter);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, UpdateAmmoCounterEvent>(OnRevolverAmmoUpdate);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, EntRemovedFromContainerMessage>(OnRevolverEntRemove);
    }

    private void OnRevolverEntRemove(Entity<RevolverAmmoProviderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != RevolverContainer)
            return;

        // <See ChamberMagazineAmmoProvider>
        if (!IsClientSide(args.Entity))
            return;

        QueueDel(args.Entity);
    }

    private void OnRevolverAmmoUpdate(Entity<RevolverAmmoProviderComponent> ent, ref UpdateAmmoCounterEvent args)
    {
        // Lust edit - передаем обновление индикатора барабана в fork partial
        UpdateLustRevolverAmmo(ent, ref args);
    }

    private void OnRevolverCounter(Entity<RevolverAmmoProviderComponent> ent, ref AmmoCounterControlEvent args)
    {
        // Lust edit - создаем индикатор барабана в fork partial
        args.Control = CreateLustRevolverCounter();
    }
}
