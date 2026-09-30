using Content.Shared._Lust.SpriteColor;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._Lust.SpriteColor;

/// <summary>
/// Client-side visualizer that applies runtime colors to specific sprite states.
/// Works in conjunction with SpriteColorComponent on the server.
/// </summary>
public sealed class SpriteColorVisualizerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpriteColorComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, SpriteColorComponent component, ComponentInit args)
    {
        ApplyColors(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Check for any entities with SpriteColorComponent and apply their colors
        var query = EntityQueryEnumerator<SpriteColorComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            ApplyColors(uid, component);
        }
    }

    private void ApplyColors(EntityUid uid, SpriteColorComponent component)
    {
        // Apply colors to sprite states
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        foreach (var (stateName, color) in component.StateColors)
        {
            // Find the layer with this state name and apply color
            foreach (var spriteLayer in sprite.AllLayers)
            {
                if (spriteLayer is not Layer layer)
                    continue;

                if (layer.State.Name == stateName)
                {
                    layer.Color = color;
                }
            }
        }
    }
}

