using Content.Shared._Lust.Thermite.Components;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Thermite.Effects;

[UsedImplicitly]
public sealed partial class DissolvableReaction : EntityEffectBase<DissolvableReaction>
{
    [DataField]
    public float Multiplier = 1f;

    public override LogImpact? Impact => LogImpact.Medium;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-dissolvable-reaction", ("chance", Probability));
}
