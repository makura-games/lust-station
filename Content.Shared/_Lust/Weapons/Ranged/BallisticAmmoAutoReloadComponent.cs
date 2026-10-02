namespace Content.Shared._Lust.Weapons.Ranged;

[RegisterComponent]
public sealed partial class BallisticAmmoAutoReloadComponent : Component
{
    [DataField]
    public TimeSpan ReloadDelay = TimeSpan.FromMinutes(2);

    public TimeSpan NextReload;
}
