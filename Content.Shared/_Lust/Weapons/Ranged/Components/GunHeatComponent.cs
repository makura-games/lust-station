using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true, raiseAfterAutoHandleState: true)]
public sealed partial class GunHeatComponent : Component
{
    [DataField]
    public float HeatPerShot = 750f;

    [ViewVariables, AutoNetworkedField]
    public float TemperaturePerShot;

    [DataField]
    public float CoolingEfficiency = 0.05f;

    [DataField]
    public float PassiveCooling = 2.5f;

    [DataField]
    public float GlowTemperature = Atmospherics.T20C + 50f;

    [DataField]
    public float JamTemperature = Atmospherics.T20C + 160f;

    [DataField]
    public float MaxJamChance = 0.35f;

    [DataField]
    public float MeltTemperature = Atmospherics.T20C + 360f;

    [AutoNetworkedField]
    public float Temperature = Atmospherics.T20C;

    [AutoNetworkedField]
    public bool Jammed;

    [AutoNetworkedField]
    public int ShotCount;

    [DataField]
    public SoundSpecifier JamSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Empty/empty.ogg");

    [DataField]
    public SoundSpecifier UnjamSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/smg_cock.ogg");

    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(1);

    [AutoNetworkedField]
    public TimeSpan NextPopupTime;
}
