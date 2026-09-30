using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Shared._Lust.SpriteColor;

/// <summary>
/// Shared system that applies runtime colors to specific sprite states.
/// Coordinates with the client-side visualizer to display color changes.
/// </summary>
public sealed class SpriteColorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Apply a color to a specific sprite state on an entity.
    /// The actual rendering happens on the client via SpriteColorVisualizerSystem.
    /// </summary>
    public void SetStateColor(EntityUid entity, string spriteName, Color color)
    {
        var spriteColorComponent = EnsureComp<SpriteColorComponent>(entity);
        spriteColorComponent.StateColors[spriteName] = color;
        Dirty(entity, spriteColorComponent);
    }

    /// <summary>
    /// Clear the color from a specific sprite state.
    /// </summary>
    public void ClearStateColor(EntityUid entity, string spriteName)
    {
        if (TryComp<SpriteColorComponent>(entity, out var component))
        {
            if (component.StateColors.Remove(spriteName))
            {
                Dirty(entity, component);
            }
        }
    }

    /// <summary>
    /// Clear all colors on this entity.
    /// </summary>
    public void ClearAllColors(EntityUid entity)
    {
        if (TryComp<SpriteColorComponent>(entity, out var component))
        {
            if (component.StateColors.Count > 0)
            {
                component.StateColors.Clear();
                Dirty(entity, component);
            }
        }
    }
}
