using Content.Shared._Lust.Weapons.Ranged.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared._Lust.Weapons.Ranged.Systems;

public abstract partial class SharedGunHeatSystem : EntitySystem
{
    [Dependency] protected IGameTiming _timing = default!;
    [Dependency] protected SharedAudioSystem _audio = default!;
    [Dependency] protected SharedPopupSystem _popup = default!;

    private const int JamSalt = -3;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunHeatComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunHeatComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<GunHeatComponent, UseInHandEvent>(OnUseInHand,
            before: [typeof(SharedWieldableSystem), typeof(SharedGunSystem)]);
        SubscribeLocalEvent<GunHeatComponent, ExaminedEvent>(OnExamined);
    }

    public static float GetGlow(GunHeatComponent comp, float temperature)
    {
        var range = comp.MeltTemperature - comp.GlowTemperature;
        return range <= 0f ? 0f : Math.Clamp((temperature - comp.GlowTemperature) / range, 0f, 1f);
    }

    public static float GetJamChance(GunHeatComponent comp, float temperature)
    {
        var range = comp.MeltTemperature - comp.JamTemperature;
        if (temperature < comp.JamTemperature || range <= 0f)
            return 0f;

        return comp.MaxJamChance * Math.Clamp((temperature - comp.JamTemperature) / range, 0f, 1f);
    }

    protected virtual void AddShotHeat(Entity<GunHeatComponent> ent, int shots)
    {
    }

    private void OnGunShot(Entity<GunHeatComponent> ent, ref GunShotEvent args)
    {
        if (args.Ammo.Count == 0)
            return;

        ent.Comp.Temperature += ent.Comp.TemperaturePerShot * args.Ammo.Count;
        AddShotHeat(ent, args.Ammo.Count);
        ent.Comp.ShotCount += args.Ammo.Count;
        DirtyField(ent, ent.Comp, nameof(GunHeatComponent.ShotCount));

        if (!ent.Comp.Jammed)
        {
            if (GetJamRoll(ent) < GetJamChance(ent.Comp, ent.Comp.Temperature))
            {
                ent.Comp.Jammed = true;
                ent.Comp.NextPopupTime = _timing.CurTime + ent.Comp.PopupCooldown;
                _audio.PlayPredicted(ent.Comp.JamSound, ent, args.User);
                _popup.PopupPredicted(Loc.GetString("lust-gun-heat-jammed"), ent, args.User, PopupType.MediumCaution);
                DirtyField(ent, ent.Comp, nameof(GunHeatComponent.Jammed));
                DirtyField(ent, ent.Comp, nameof(GunHeatComponent.NextPopupTime));
            }
        }

        DirtyField(ent, ent.Comp, nameof(GunHeatComponent.Temperature));
    }

    private float GetJamRoll(Entity<GunHeatComponent> ent)
    {
        var seed = unchecked((uint) _timing.CurTick.Value
                             ^ (uint) GetNetEntity(ent).Id
                             ^ (uint) ent.Comp.ShotCount * 0x9e3779b9
                             ^ (uint) JamSalt);
        seed ^= seed >> 16;
        seed *= 0x7feb352d;
        seed ^= seed >> 15;
        seed *= 0x846ca68b;
        seed ^= seed >> 16;
        return (seed >> 8) * (1f / 16777216f);
    }

    private void OnAttemptShoot(Entity<GunHeatComponent> ent, ref AttemptShootEvent args)
    {
        if (!TryShoot(ent, args.User))
            args.Cancelled = true;
    }

    private void OnUseInHand(Entity<GunHeatComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryUnjam(ent, args.User);
    }

    public bool TryShoot(Entity<GunHeatComponent> ent, EntityUid user)
    {
        if (CanShoot(ent))
            return true;

        if (_timing.CurTime < ent.Comp.NextPopupTime)
            return false;

        ent.Comp.NextPopupTime = _timing.CurTime + ent.Comp.PopupCooldown;
        DirtyField(ent, ent.Comp, nameof(GunHeatComponent.NextPopupTime));
        _popup.PopupPredicted(Loc.GetString("lust-gun-heat-jammed-blocked"), ent, user, PopupType.SmallCaution);
        return false;
    }

    public bool CanShoot(Entity<GunHeatComponent> ent)
        => !ent.Comp.Jammed;

    public bool TryUnjam(Entity<GunHeatComponent> ent, EntityUid user)
    {
        if (!CanUnjam(ent))
            return false;

        Unjam(ent, user);
        return true;
    }

    public bool CanUnjam(Entity<GunHeatComponent> ent)
        => ent.Comp.Jammed;

    private void Unjam(Entity<GunHeatComponent> ent, EntityUid user)
    {
        ent.Comp.Jammed = false;
        DirtyField(ent, ent.Comp, nameof(GunHeatComponent.Jammed));

        _audio.PlayPredicted(ent.Comp.UnjamSound, ent, user);
        _popup.PopupPredicted(Loc.GetString("lust-gun-heat-unjammed"), ent, user);
    }

    private void OnExamined(Entity<GunHeatComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.Jammed)
            args.PushMarkup(Loc.GetString("lust-gun-heat-examine-jammed"));

        if (ent.Comp.Temperature >= ent.Comp.JamTemperature)
            args.PushMarkup(Loc.GetString("lust-gun-heat-examine-hot"));
    }
}
