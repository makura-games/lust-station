using Content.Shared._Lust.LockableEquipment;
using Robust.Client.GameObjects;

namespace Content.Client._Lust.LockableEquipment;

public sealed class LockableEquipmentVisualizerSystem : VisualizerSystem<LockableEquipmentComponent>
{
    private const string BaseLayer = "base";

    protected override void OnAppearanceChange(EntityUid uid, LockableEquipmentComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), BaseLayer, out var layer, false))
            return;

        if (!AppearanceSystem.TryGetData<string>(uid, EquipmentVisuals.IconState, out var iconState, args.Component) ||
            string.IsNullOrEmpty(iconState))
        {
            return;
        }

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), layer, iconState);
    }
}
