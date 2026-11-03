using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared._Sunrise;

public sealed partial class BodyTypePrototype
{
    private bool TryGetLustFallbackLayer(HumanoidVisualLayers layer, Sex sex, out PrototypeLayerData data)
    {
        data = default!;

        if (sex != Sex.Futanari ||
            !SexLayers.TryGetValue(Sex.Female, out var femaleLayers))
        {
            return false;
        }

        return femaleLayers.TryGetValue(layer, out data!);
    }
}
