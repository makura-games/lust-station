using Content.Server._Lust.Swarmer.Components;
using Content.Shared.Cuffs.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._Lust.Swarmer;

public sealed partial class SwarmerSystem
{
    private void InitializeGlow()
    {
        SubscribeLocalEvent<SwarmerGlowWhileCuffedComponent, EntGotInsertedIntoContainerMessage>(OnGlowCuffsInserted);
        SubscribeLocalEvent<SwarmerGlowWhileCuffedComponent, EntGotRemovedFromContainerMessage>(OnGlowCuffsRemoved);
        SubscribeLocalEvent<SwarmerGlowWhileCuffedComponent, ComponentShutdown>(OnGlowCuffsShutdown);
    }

    private void OnGlowCuffsInserted(Entity<SwarmerGlowWhileCuffedComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        var wearer = args.Container.Owner;
        if (!HasComp<CuffableComponent>(wearer))
            return;

        // Не перехватываем свет, который у носящего уже есть.
        if (HasComp<PointLightComponent>(wearer))
            return;

        var light = PointLight.EnsureLight(wearer);
        PointLight.SetColor(wearer, ent.Comp.Color, light);
        PointLight.SetRadius(wearer, ent.Comp.Radius, light);
        PointLight.SetEnergy(wearer, ent.Comp.Energy, light);
        PointLight.SetEnabled(wearer, true, light);
        ent.Comp.LitWearer = wearer;
    }

    private void OnGlowCuffsRemoved(Entity<SwarmerGlowWhileCuffedComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        RemoveGlow(ent);
    }

    private void OnGlowCuffsShutdown(Entity<SwarmerGlowWhileCuffedComponent> ent, ref ComponentShutdown args)
    {
        RemoveGlow(ent);
    }

    private void RemoveGlow(Entity<SwarmerGlowWhileCuffedComponent> ent)
    {
        if (ent.Comp.LitWearer is not { } wearer)
            return;

        ent.Comp.LitWearer = null;
        if (!TerminatingOrDeleted(wearer))
            RemComp<PointLightComponent>(wearer);
    }
}
