using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Traits.Assorted;

public sealed partial class MobilityAidSystem
{
    [SubscribeLocalEvent]
    private void OnMobilityAidToggled(Entity<MobilityAidComponent> ent, ref ItemToggledEvent args)
    {
        var user = args.User ?? Transform(ent).ParentUid;
        if (user.IsValid())
            _movementSpeedModifier.RefreshMovementSpeedModifiers(user);
    }
}
