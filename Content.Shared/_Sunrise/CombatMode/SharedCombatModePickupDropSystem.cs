using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Item;

namespace Content.Shared._Sunrise.CombatMode;

public abstract class SharedCombatModePickupDropSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CombatModeComponent, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<CombatModeComponent, DropAttemptEvent>(OnDropAttempt);
    }

    private void OnPickupAttempt(EntityUid uid, CombatModeComponent component, PickupAttemptEvent args)
    {
        if (ShouldBlockPickup(uid, component))
            args.Cancel();
    }

    private void OnDropAttempt(EntityUid uid, CombatModeComponent component, DropAttemptEvent args)
    {
        if (ShouldBlockDrop(uid, component))
            args.Cancel();
    }

    protected abstract bool ShouldBlockPickup(EntityUid uid, CombatModeComponent component);

    protected abstract bool ShouldBlockDrop(EntityUid uid, CombatModeComponent component);
}
