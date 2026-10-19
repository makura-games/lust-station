using Content.Shared._Lust.Swarmer;
using Content.Shared._Lust.Swarmer.Components;
using Content.Shared.Mind.Components;

namespace Content.Server._Lust.Swarmer;

public sealed partial class SwarmerSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private void InitializeDormancy()
    {
        SubscribeLocalEvent<SwarmerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SwarmerComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnMindAdded(Entity<SwarmerComponent> ent, ref MindAddedMessage args)
    {
        SetActive(ent, true);
    }

    private void OnMindRemoved(Entity<SwarmerComponent> ent, ref MindRemovedMessage args)
    {
        SetActive(ent, false);
    }

    protected override void OnSwarmerMapInit(Entity<SwarmerComponent> ent)
    {
        SetActive(ent, TryComp<MindContainerComponent>(ent, out var mind) && mind.HasMind);
    }

    /// <summary>
    /// A swarmer without a player is a dormant shell: dormant sprite, no light.
    /// </summary>
    private void SetActive(EntityUid uid, bool active)
    {
        _appearance.SetData(uid, SwarmerVisuals.Active, active);

        if (PointLight.TryGetLight(uid, out var light))
            PointLight.SetEnabled(uid, active, light);
    }
}
