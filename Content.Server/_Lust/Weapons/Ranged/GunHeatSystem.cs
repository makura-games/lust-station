using Content.Server.Atmos.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._Lust.Weapons.Ranged.Components;
using Content.Shared._Lust.Weapons.Ranged.Systems;
using Content.Shared.Atmos;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;

namespace Content.Server._Lust.Weapons.Ranged;

public sealed partial class GunHeatSystem : SharedGunHeatSystem
{
    [Dependency] private TemperatureSystem _temperature = default!;

    private const float SyncStep = 2f;
    private static readonly TimeSpan PassiveCoolingInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SyncHold = TimeSpan.FromSeconds(2);

    private readonly Dictionary<EntityUid, TimeSpan> _lastShot = new();
    private TimeSpan _nextPassiveCooling;
    private bool _firing;

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GunHeatComponent> ent, ref ComponentShutdown args)
    {
        _lastShot.Remove(ent);
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<GunHeatComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<TemperatureComponent>(ent);
        EnsureComp<AtmosExposedComponent>(ent);
    }

    [SubscribeLocalEvent(after: [typeof(TemperatureSystem)])]
    private void OnMapInit(Entity<GunHeatComponent> ent, ref MapInitEvent args)
    {
        var temperature = EnsureComp<TemperatureComponent>(ent);
        EnsureComp<AtmosExposedComponent>(ent);

        ent.Comp.TemperaturePerShot = temperature.HeatCapacity > 0f
            ? ent.Comp.HeatPerShot / temperature.HeatCapacity
            : 0f;
        Sync(ent, temperature.Temperature, true);
    }

    [SubscribeLocalEvent]
    private void OnTemperatureChange(Entity<GunHeatComponent> ent, ref TemperatureChangedEvent args)
    {
        if (!_firing && args.TemperatureDelta > 0f && args.CurrentTemperature > ent.Comp.JamTemperature - 20f
            && TryComp<TemperatureComponent>(ent, out var temperature))
        {
            var excessHeat = (args.CurrentTemperature - (ent.Comp.JamTemperature - 20f))
                             * temperature.HeatCapacity;
            _temperature.ChangeHeat((ent.Owner, temperature), -excessHeat, ignoreHeatResistance: true);
            return;
        }

        if (MathF.Abs(args.CurrentTemperature - ent.Comp.Temperature) < SyncStep)
            return;

        if (_lastShot.TryGetValue(ent, out var lastShot) && _timing.CurTime < lastShot + SyncHold)
            return;

        ent.Comp.Temperature = args.CurrentTemperature;
        DirtyField(ent, ent.Comp, nameof(GunHeatComponent.Temperature));
    }

    protected override void AddShotHeat(Entity<GunHeatComponent> ent, int shots)
    {
        if (!TryComp<TemperatureComponent>(ent, out var temperature))
            return;

        _lastShot[ent] = _timing.CurTime;
        _firing = true;
        try
        {
            _temperature.ChangeHeat((ent.Owner, temperature), ent.Comp.HeatPerShot * shots, ignoreHeatResistance: true);
        }
        finally
        {
            _firing = false;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_timing.CurTime < _nextPassiveCooling)
            return;

        _nextPassiveCooling = _timing.CurTime + PassiveCoolingInterval;
        var query = EntityQueryEnumerator<GunHeatComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var heat, out var temperature))
        {
            var excess = temperature.Temperature - Atmospherics.T20C;
            if (excess <= 0.5f)
                continue;

            var heatLoss = MathF.Min(excess * heat.PassiveCooling, excess * temperature.HeatCapacity);
            _temperature.ChangeHeat(uid, -heatLoss, ignoreHeatResistance: true);
        }
    }

    private void Sync(Entity<GunHeatComponent> ent, float temperature, bool force = false)
    {
        if (!force && MathF.Abs(temperature - ent.Comp.Temperature) < SyncStep)
            return;

        if (!force && _lastShot.TryGetValue(ent, out var lastShot) && _timing.CurTime < lastShot + SyncHold)
            return;

        ent.Comp.Temperature = temperature;
        DirtyField(ent, ent.Comp, nameof(GunHeatComponent.Temperature));
    }
}
