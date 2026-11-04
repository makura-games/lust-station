using Content.Server._Sunrise.Animations;
using Content.Shared._Lust.Swarmer.Components;
using Content.Shared._Sunrise.Animations;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.Swarmer;

public sealed partial class SwarmerSystem
{
    [Dependency] private EmoteAnimationSystem _emoteAnimation = default!;

    private static readonly ProtoId<EmotePrototype> FlipEmote = "Flip";

    /// <summary>
    /// The swarmer does a silent flip with every hit: the emote animation plays without a chat message.
    /// </summary>
    protected override void OnSwarmerHit(Entity<SwarmerComponent> ent)
    {
        if (TryComp<EmoteAnimationComponent>(ent, out var animation))
            _emoteAnimation.PlayEmoteAnimation(ent, animation, FlipEmote);
    }
}
