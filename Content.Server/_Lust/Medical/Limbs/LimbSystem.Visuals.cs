using Content.Shared.Damage;
using Content.Shared.Humanoid;

namespace Content.Server._Starlight.Medical.Limbs;

public sealed partial class LimbSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    /// <summary>
    /// Восстанавливает видимость волос и оверлеев при возвращении головы и конечностей.
    /// </summary>
    private void OnLimbVisualAddedLust(Entity<HumanoidProfileComponent?> body, HumanoidVisualLayers layer)
    {
        if (layer == HumanoidVisualLayers.Head)
        {
            _sunriseBody.SetLayersVisibility(body.Owner, [
                HumanoidVisualLayers.Hair,
                HumanoidVisualLayers.FacialHair,
                HumanoidVisualLayers.HeadTop,
                HumanoidVisualLayers.HeadSide
            ], true);
        }

        if (TryComp<AppearanceComponent>(body.Owner, out var appearance))
        {
            _appearance.SetData(body.Owner, layer, false, appearance);
            _appearance.SetData(body.Owner, DamageVisualizerKeys.ForceUpdate, true, appearance);
        }
    }

    /// <summary>
    /// Скрывает волосы и слой крови/урона, чтобы не оставался призрак конечности.
    /// </summary>
    private void OnLimbVisualRemovedLust(Entity<HumanoidProfileComponent?> body, HumanoidVisualLayers layer)
    {
        if (layer == HumanoidVisualLayers.Head)
        {
            _sunriseBody.SetLayersVisibility(body.Owner, [
                HumanoidVisualLayers.Hair,
                HumanoidVisualLayers.FacialHair,
                HumanoidVisualLayers.HeadTop,
                HumanoidVisualLayers.HeadSide
            ], false);
        }

        if (TryComp<AppearanceComponent>(body.Owner, out var appearance))
        {
            _appearance.SetData(body.Owner, layer, true, appearance);
            _appearance.SetData(body.Owner, DamageVisualizerKeys.ForceUpdate, true, appearance);
        }
    }
}
