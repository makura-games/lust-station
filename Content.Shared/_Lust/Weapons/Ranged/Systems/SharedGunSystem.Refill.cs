using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    /// <summary>
    /// Убирает все патроны и заполняет до вместимости неспавненными.
    /// </summary>
    public void RefillBallisticAmmo(Entity<BallisticAmmoProviderComponent> entity)
    {
        if (entity.Comp.Proto == null)
            return;

        foreach (var ent in entity.Comp.Entities)
        {
            Containers.Remove(ent, entity.Comp.Container);
            PredictedQueueDel(ent);
        }

        entity.Comp.Entities.Clear();
        entity.Comp.UnspawnedCount = entity.Comp.Capacity;
        UpdateBallisticAppearance(entity);
        UpdateAmmoCount(entity.Owner);
        Dirty(entity);
    }

    /// <summary>
    /// Убирает отстрелянные гильзы и снова заряжает все камеры барабана.
    /// </summary>
    public void RefillRevolver(Entity<RevolverAmmoProviderComponent> entity)
    {
        var comp = entity.Comp;

        for (var i = 0; i < comp.AmmoSlots.Count; i++)
        {
            if (comp.AmmoSlots[i] is { } spent)
            {
                Containers.Remove(spent, comp.AmmoContainer);
                PredictedQueueDel(spent);
                comp.AmmoSlots[i] = null;
            }

            if (i < comp.Chambers.Length)
                comp.Chambers[i] = true;
        }

        UpdateRevolverAppearance(entity);
        UpdateAmmoCount(entity, prediction: false);
        Dirty(entity);
    }
}
