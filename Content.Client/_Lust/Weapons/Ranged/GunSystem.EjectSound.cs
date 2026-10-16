using Content.Shared.Weapons.Ranged.Components;

#pragma warning disable IDE0130
namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    private void PlayLustCartridgeEjectSound(
        EntityUid gunUid,
        EntityUid cartridgeUid,
        CartridgeAmmoComponent cartridge,
        GunComponent gun,
        EntityUid? user)
    {
        if (cartridge.DeleteOnSpawn || Containers.IsEntityInContainer(cartridgeUid) || gun.Pump)
            return;

        Audio.PlayPredicted(cartridge.EjectSound, gunUid, user);
    }
}
