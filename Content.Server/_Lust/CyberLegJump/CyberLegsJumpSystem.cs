using Content.Server._Starlight.Medical.Limbs;
using Content.Shared.Body;
using Content.Shared.Movement.Components;

namespace Content.Shared._Lust.CyberLegsJump;

public sealed partial class CyberLegsJumpSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, LimbAttachedEvent<CyberLegComponent>>(OnCyberLegAttached);
        SubscribeLocalEvent<BodyComponent, LimbRemovedEvent<CyberLegComponent>>(OnCyberLegRemoved);
    }

    private void OnCyberLegAttached(Entity<BodyComponent> ent, ref LimbAttachedEvent<CyberLegComponent> args)
    {
        UpdateJump(ent);
    }

    private void OnCyberLegRemoved(Entity<BodyComponent> ent, ref LimbRemovedEvent<CyberLegComponent> args)
    {
        UpdateJump(ent);
    }

    private void UpdateJump(Entity<BodyComponent> ent)
    {
        var hasLeft = HasCyberLeg(ent, "LegLeft");
        var hasRight = HasCyberLeg(ent, "LegRight");

        if (hasLeft && hasRight)
            EnsureComp<JumpAbilityComponent>(ent);
        else
            RemComp<JumpAbilityComponent>(ent);
    }

    private bool HasCyberLeg(Entity<BodyComponent> ent, string category)
    {
        if (ent.Comp.Organs == null)
            return false;

        foreach (var organ in ent.Comp.Organs.ContainedEntities)
        {
            if (!TryComp<OrganComponent>(organ, out var organComp))
                continue;

            if (organComp.Category?.Id != category)
                continue;

            if (HasComp<CyberLegComponent>(organ))
                return true;
        }

        return false;
    }
}
