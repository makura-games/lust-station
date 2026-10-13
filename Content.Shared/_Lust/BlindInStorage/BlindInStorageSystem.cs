using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Storage.Components;
using Robust.Shared.GameObjects;
using Content.Shared.Eye.Blinding.Components;
public sealed partial class BlindInStorageSystem : EntitySystem
{
    [Dependency] private BlindableSystem _blindable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InsideEntityStorageComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<InsideEntityStorageComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<InsideEntityStorageComponent, CanSeeAttemptEvent>(OnCanSee);
    }

    private void OnStartup(Entity<InsideEntityStorageComponent> ent, ref ComponentStartup args)
    {
        _blindable.UpdateIsBlind(ent.Owner);
    }

    private void OnShutdown(Entity<InsideEntityStorageComponent> ent, ref ComponentShutdown args)
    {
        _blindable.UpdateIsBlind(ent.Owner);
    }

    private void OnCanSee(Entity<InsideEntityStorageComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        if (!HasComp<BlindableComponent>(ent))
            return;

        args.Cancel();
    }

}
