using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._Lust.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Timing;

namespace Content.Server._Lust.Weapons.Ranged;

public sealed partial class BallisticAmmoAutoReloadSystem : EntitySystem
{
    [Dependency] private GunSystem _gun = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BallisticAmmoAutoReloadComponent, BallisticAmmoProviderComponent>();

        while (query.MoveNext(out var uid, out var reload, out var ammo))
        {
            if (_timing.CurTime < reload.NextReload)
                continue;

            reload.NextReload = _timing.CurTime + reload.ReloadDelay;

            var missingAmmo = ammo.Capacity - ammo.Count;
            if (missingAmmo <= 0)
                continue;

            _gun.SetBallisticUnspawned((uid, ammo), ammo.UnspawnedCount + missingAmmo);
        }
    }
}
