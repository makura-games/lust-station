using Content.Server.Chat.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Timing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Lust.Chat;

public sealed class EmoteOnUseSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmoteOnUseComponent, UseInHandEvent>(OnUseInHand);
    }

    public void OnUseInHand(EntityUid uid, EmoteOnUseComponent? component, UseInHandEvent args)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp<UseDelayComponent>(uid, out var useDelayComponent) || _useDelay.IsDelayed((uid, useDelayComponent)))
            return;

        _chat.TryEmoteWithChat(uid, _random.Pick(component.Values));
        _useDelay.TryResetDelay((uid, useDelayComponent));
    }
}
