using Content.Shared._Lust.Swarmer;
using Content.Shared._Lust.Swarmer.Components;
using Content.Shared._Lust.Swarmer.Systems;
using Content.Shared.Alert.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Physics.Components;

namespace Content.Client._Lust.Swarmer;

public sealed partial class SwarmerSystem : SharedSwarmerSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    /// <summary>
    /// Squared speed above which the swarmer counts as moving.
    /// </summary>
    private const float MovingSpeedSquared = 0.01f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwarmerComponent, GetGenericAlertCounterAmountEvent>(OnGetCounterAmount);
    }

    /// <summary>
    /// Picks the walk or idle animation from the swarmer's actual velocity every frame.
    /// The local player's velocity is predicted, so the animation reacts to a key press immediately.
    /// </summary>
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<SwarmerComponent, SpriteComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var swarmer, out var sprite, out var physics))
        {
            if (!_sprite.LayerMapTryGet((uid, sprite), SwarmerVisualLayers.Body, out var layer, false))
                continue;

            var state = physics.LinearVelocity.LengthSquared() > MovingSpeedSquared
                ? swarmer.MovingState
                : swarmer.IdleState;

            // Повторная установка того же состояния сбросила бы анимацию на первый кадр.
            if (_sprite.LayerGetRsiState((uid, sprite), layer).Name == state)
                continue;

            _sprite.LayerSetRsiState((uid, sprite), layer, state);
        }
    }

    private void OnGetCounterAmount(Entity<SwarmerComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled || ent.Comp.ResourceAlert != args.Alert.ID)
            return;

        args.Amount = ent.Comp.Resources;
    }
}
