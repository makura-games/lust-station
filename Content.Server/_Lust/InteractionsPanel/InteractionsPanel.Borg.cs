using Content.Shared._Lust.Borgs;
using Content.Shared._Lust.Borgs.Components;
using Content.Shared.Humanoid;

#pragma warning disable IDE0130
namespace Content.Server._Sunrise.InteractionsPanel;

public partial class InteractionsPanel
{
    private partial void InitializeLustInteractions()
    {
        SubscribeLocalEvent<BorgSexComponent, BorgSexChangedEvent>(OnBorgSexChanged);
    }

    private partial string GetLustInteractionSex(EntityUid uid)
    {
        return BorgSexSystem.TryGetSex(uid, EntityManager, out var sex)
            ? sex.ToString().ToLowerInvariant()
            : "unknown";
    }

    private partial bool ShouldSpawnLustSemen(EntityUid uid)
    {
        return BorgSexSystem.TryGetSex(uid, EntityManager, out var sex) && sex == Sex.Male;
    }

    private void OnBorgSexChanged(Entity<BorgSexComponent> ent, ref BorgSexChangedEvent args)
    {
        UpdateUIForClothingChange(ent.Owner);
    }
}
#pragma warning restore IDE0130
