using Content.Shared._Lust.LockableEquipment;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Lust.LockableEquipment;

public sealed class EquipmentVisualizerSystem : VisualizerSystem<EquipmentContainerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, EquipmentContainerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = args.Sprite;

        // Hide the previously active layer (tracked in visual data) to avoid stale overlays
        // when the device is removed or swapped.
        if (AppearanceSystem.TryGetData<EquipmentVisualData>(uid, EquipmentVisuals.PreviousLayer, out var prev, args.Component) &&
            prev?.Layer != null &&
            SpriteSystem.LayerMapTryGet((uid, sprite), prev.Layer, out var prevIdx, false))
        {
            SpriteSystem.LayerSetVisible((uid, sprite), prevIdx, false);
        }

        if (!AppearanceSystem.TryGetData<EquipmentVisualData>(uid, EquipmentVisuals.VisualData, out var visualData, args.Component) ||
            visualData == null ||
            !visualData.Visible ||
            string.IsNullOrEmpty(visualData.Layer) ||
            string.IsNullOrEmpty(visualData.RsiPath) ||
            string.IsNullOrEmpty(visualData.State))
        {
            return;
        }

        var layerIdx = SpriteSystem.LayerMapTryGet((uid, sprite), visualData.Layer, out var existingIdx, false)
            ? existingIdx
            : SpriteSystem.LayerMapReserve((uid, sprite), visualData.Layer);

        SpriteSystem.LayerSetRsi((uid, sprite), layerIdx, new ResPath(visualData.RsiPath));
        SpriteSystem.LayerSetRsiState((uid, sprite), layerIdx, visualData.State);
        SpriteSystem.LayerSetVisible((uid, sprite), layerIdx, true);
    }
}
