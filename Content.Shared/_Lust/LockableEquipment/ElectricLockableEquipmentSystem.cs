using Content.Shared.Trigger;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Lust.LockableEquipment;

/// <summary>
/// Temporarily switches the cage item's icon state to the activated animation on trigger, then restores it.
/// </summary>
public sealed class ElectricLockableEquipmentSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LockableEquipmentSystem _lockable = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ElectricLockableEquipmentComponent, TriggerEvent>(OnTrigger);
    }

    public override void Update(float frameTime)
    {
        if (!_net.IsServer)
            return;

        var query = EntityQueryEnumerator<ElectricLockableEquipmentComponent, LockableEquipmentComponent>();
        while (query.MoveNext(out var uid, out var electric, out var lockable))
        {
            if (electric.ActivatedUntil == TimeSpan.Zero || _timing.CurTime < electric.ActivatedUntil)
                continue;

            electric.ActivatedUntil = TimeSpan.Zero;
            _lockable.RefreshIconState((uid, lockable));
        }
    }

    private void OnTrigger(Entity<ElectricLockableEquipmentComponent> ent, ref TriggerEvent args)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        ent.Comp.ActivatedUntil = _timing.CurTime + ent.Comp.ActivationDuration;
        _appearance.SetData(ent, EquipmentVisuals.IconState, ent.Comp.ActivatedIconState, appearance);
    }
}
