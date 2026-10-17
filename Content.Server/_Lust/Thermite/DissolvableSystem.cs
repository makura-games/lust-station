using Content.Server.Temperature.Systems;
using Content.Shared._Lust.Thermite.Components;
using Content.Shared._Lust.Thermite.Systems;
using Content.Shared.Atmos;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Temperature;
using Content.Shared.Tools.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.Timing;

namespace Content.Server._Lust.Thermite;

public sealed partial class DissolvableSystem : SharedDissolvableSystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TemperatureSystem _temperature = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThermiteComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<DissolvableComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnInteractUsing(Entity<ThermiteComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryIgniteThermite(ent, args.Used))
            return;

        args.Handled = true;
    }

    private void OnDestruction(Entity<DissolvableComponent> ent, ref DestructionEventArgs args)
    {
        Extinguish(ent);
    }

    public bool TryIgniteThermite(Entity<ThermiteComponent> thermite, EntityUid tool)
    {
        if (!CanIgniteThermite(thermite, tool))
            return false;

        IgniteThermite(thermite);
        return true;
    }

    public bool CanIgniteThermite(Entity<ThermiteComponent> thermite, EntityUid tool)
    {
        if (!HasComp<WelderComponent>(tool))
            return false;

        var isHot = new IsHotEvent();
        RaiseLocalEvent(tool, isHot);
        return isHot.IsHot;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveDissolvableComponent, DissolvableComponent>();
        while (query.MoveNext(out var uid, out _, out var dissolvable))
        {
            if (!dissolvable.IsBurning)
            {
                RemCompDeferred<ActiveDissolvableComponent>(uid);
                continue;
            }

            if (_timing.CurTime < dissolvable.NextUpdate)
                continue;

            dissolvable.NextUpdate = _timing.CurTime + UpdateInterval;

            if (dissolvable.Stacks <= 0f)
            {
                Extinguish((uid, dissolvable));
                continue;
            }

            if (TryComp(uid, out TemperatureComponent? temperature))
                _temperature.ChangeHeat(uid, 12500f * dissolvable.Stacks, false, temperature);

            _damageable.TryChangeDamage(uid, dissolvable.Damage * dissolvable.Stacks, interruptsDoAfters: false);
            if (!TryComp<DissolvableComponent>(uid, out var updated) || !updated.IsBurning)
                continue;

            AdjustDissolveStacks(uid, updated.StackFade, updated);
            if (updated.Stacks <= 0f)
                Extinguish((uid, updated));
        }
    }

    private void IgniteThermite(Entity<ThermiteComponent> thermite)
    {
        var coordinates = _transform.GetMapCoordinates(thermite);
        foreach (var target in _lookup.GetEntitiesInRange<DissolvableComponent>(coordinates, 1f))
        {
            if (target.Comp.Stacks <= 0f || target.Comp.IsBurning)
                continue;

            IgniteDissolvable(target);
        }

        foreach (var mark in _lookup.GetEntitiesInRange<ThermiteComponent>(coordinates, 1f))
            QueueDel(mark);
    }

    private void IgniteDissolvable(Entity<DissolvableComponent> ent)
    {
        ent.Comp.IsBurning = true;
        ent.Comp.NextUpdate = _timing.CurTime + UpdateInterval;
        DirtyField(ent, ent.Comp, nameof(DissolvableComponent.IsBurning));
        EnsureComp<ActiveDissolvableComponent>(ent);

        ent.Comp.FireEffect = Spawn("ThermiteFire", _transform.GetMapCoordinates(ent));
        var ignited = new IgnitedEvent();
        RaiseLocalEvent(ent, ref ignited);
    }

    private void Extinguish(Entity<DissolvableComponent> ent)
    {
        if (!ent.Comp.IsBurning)
        {
            RemCompDeferred<ActiveDissolvableComponent>(ent);
            return;
        }

        ent.Comp.IsBurning = false;
        ent.Comp.Stacks = 0f;
        DirtyField(ent, ent.Comp, nameof(DissolvableComponent.IsBurning));
        DirtyField(ent, ent.Comp, nameof(DissolvableComponent.Stacks));

        if (ent.Comp.FireEffect is { } effect && !Deleted(effect))
            QueueDel(effect);

        ent.Comp.FireEffect = null;
        RemCompDeferred<ActiveDissolvableComponent>(ent);

        var extinguished = new ExtinguishedEvent();
        RaiseLocalEvent(ent, ref extinguished);
    }
}
