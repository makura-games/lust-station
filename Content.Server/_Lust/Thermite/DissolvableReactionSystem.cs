using Content.Shared._Lust.Thermite.Components;
using Content.Shared._Lust.Thermite.Effects;
using Content.Shared._Lust.Thermite.Systems;
using Content.Shared.EntityEffects;

namespace Content.Server._Lust.Thermite;

public sealed partial class DissolvableReactionSystem : EntityEffectSystem<DissolvableComponent, DissolvableReaction>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedDissolvableSystem _dissolvable = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<DissolvableComponent> entity, ref EntityEffectEvent<DissolvableReaction> args)
    {
        _dissolvable.AdjustDissolveStacks(entity, args.Effect.Multiplier * args.Scale, entity.Comp);

        var coordinates = _transform.GetMapCoordinates(entity);
        if (_lookup.GetEntitiesInRange<ThermiteComponent>(coordinates, 1f).Count == 0)
            Spawn("ThermiteEntity", coordinates);
    }
}
