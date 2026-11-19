using System.Diagnostics.CodeAnalysis;
using Content.Shared._Lust.Swarmer.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Trigger;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._Lust.Swarmer.Systems;

public abstract partial class SharedSwarmerSystem
{
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    private void InitializeCombat()
    {
        SubscribeLocalEvent<SwarmerComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<SwarmerTrapComponent, TriggerEvent>(OnTrapTriggered);
    }

    /// <summary>
    /// Every hit sparks on the target. Cyborgs ignore stamina damage, so swarmer hits drain their battery instead.
    /// </summary>
    private void OnMeleeHit(Entity<SwarmerComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var target in args.HitEntities)
        {
            PredictedSpawnAtPosition(ent.Comp.HitEffect, Transform(target).Coordinates);
            TryDrainBorg(target, ent.Comp.BorgDrainPerHit);
        }

        OnSwarmerHit(ent);
    }

    private void OnTrapTriggered(Entity<SwarmerTrapComponent> ent, ref TriggerEvent args)
    {
        if (args.User is not { } user)
            return;

        if (TryDrainBorg(user, ent.Comp.BorgDrain))
            _stun.TryUpdateParalyzeDuration(user, ent.Comp.BorgStunDuration);
    }

    /// <summary>
    /// Drains the given fraction of a cyborg's maximum battery charge.
    /// </summary>
    public bool TryDrainBorg(EntityUid target, float fraction)
    {
        if (!CanDrainBorg(target, out var battery))
            return false;

        _battery.UseCharge(battery.Value.AsNullable(), battery.Value.Comp.MaxCharge * fraction);
        return true;
    }

    /// <summary>
    /// Whether the target is a cyborg with a power cell inserted.
    /// </summary>
    public bool CanDrainBorg(EntityUid target, [NotNullWhen(true)] out Entity<BatteryComponent>? battery)
    {
        battery = null;
        return HasComp<BorgChassisComponent>(target) && _powerCell.TryGetBatteryFromSlot(target, out battery);
    }
}
