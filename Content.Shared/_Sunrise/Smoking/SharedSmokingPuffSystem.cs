using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Smoking;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Smoking;

/// <summary>
/// Общая система для клиентского предсказания активных затяжек сигаретами, сигарами и трубками.
/// </summary>
public abstract partial class SharedSmokingPuffSystem : EntitySystem
{
    [Dependency] protected SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected SharedSolutionContainerSystem Solution = default!;
    [Dependency] protected IngestionSystem Ingestion = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmokablePuffComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<SmokablePuffComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SmokablePuffComponent, GetItemActionsEvent>(OnSmokableGetActions);
        SubscribeLocalEvent<SmokablePuffComponent, BlowSmokeRingActionEvent>(OnSmokableBlowSmokeRing);
    }

    private void OnUseInHand(Entity<SmokablePuffComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryStartPuff(entity, args.User, args.User))
            args.Handled = true;
    }

    private void OnAfterInteract(Entity<SmokablePuffComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryStartPuff(entity, args.User, args.Target.Value))
            args.Handled = true;
    }

    public bool CanPuff(
        Entity<SmokablePuffComponent> entity,
        EntityUid user,
        EntityUid target,
        out SmokableComponent? smokable,
        out Entity<SolutionComponent>? targetSoln,
        out Solution? solution,
        bool quiet = false)
    {
        smokable = null;
        targetSoln = null;
        solution = null;

        if (!TryComp<SmokableComponent>(entity.Owner, out smokable))
            return false;

        if (smokable.State != SmokableState.Lit)
            return false;

        if (!HasComp<BloodstreamComponent>(target))
            return false;

        if (!Ingestion.HasMouthAvailable(user, target))
        {
            if (!quiet)
                Popup.PopupClient(Loc.GetString("vape-component-mouth-blocked"), user, user);
            return false;
        }

        if (Timing.CurTime < entity.Comp.LastPuffTime + entity.Comp.Cooldown)
            return false;

        if (!Solution.TryGetSolution(entity.Owner, smokable.Solution, out targetSoln, out solution) ||
            solution.Volume <= FixedPoint2.Zero)
        {
            if (!quiet)
                Popup.PopupClient(Loc.GetString("smoking-puff-burnt-out", ("item", entity.Owner)), user, user);
            return false;
        }

        return true;
    }

    public bool TryStartPuff(Entity<SmokablePuffComponent> entity, EntityUid user, EntityUid target)
    {
        if (!CanPuff(entity, user, target, out _, out _, out _))
            return false;

        var forced = target != user;
        var delay = forced ? entity.Comp.ForcedPuffDelay : entity.Comp.PuffDelay;

        entity.Comp.LastPuffTime = Timing.CurTime;
        Dirty(entity);

        if (entity.Comp.PuffSound != null)
        {
            Audio.PlayPredicted(entity.Comp.PuffSound, entity.Owner, user);
        }

        if (forced)
        {
            var targetName = Identity.Entity(target, EntityManager);
            var userName = Identity.Entity(user, EntityManager);
            Popup.PopupPredicted(Loc.GetString("smoking-puff-try-forced", ("user", userName)),
                Loc.GetString("smoking-puff-try-user-forced", ("target", targetName)),
                target, user);
        }
        else
        {
            Popup.PopupPredicted(Loc.GetString("smoking-puff-try"), target, user);
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, new SmokablePuffDoAfterEvent(forced), entity.Owner, target: target, used: entity.Owner)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = true,
        };

        return DoAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnSmokableGetActions(Entity<SmokablePuffComponent> entity, ref GetItemActionsEvent args)
    {
        args.AddAction(ref entity.Comp.SmokeRingActionEntity, entity.Comp.SmokeRingAction);
    }

    private void OnSmokableBlowSmokeRing(Entity<SmokablePuffComponent> entity, ref BlowSmokeRingActionEvent args)
    {
        if (args.Handled)
            return;

        if (TryStartBlowSmokeRing(entity, args.Performer, args.Target))
            args.Handled = true;
    }

    public bool TryStartBlowSmokeRing(Entity<SmokablePuffComponent> entity, EntityUid user, EntityCoordinates target)
    {
        if (!CanPuff(entity, user, user, out _, out _, out _, quiet: false))
            return false;

        entity.Comp.LastPuffTime = Timing.CurTime;
        Dirty(entity);

        if (entity.Comp.PuffSound != null)
        {
            Audio.PlayPredicted(entity.Comp.PuffSound, entity.Owner, user);
        }

        Popup.PopupPredicted(Loc.GetString("smoking-puff-try"), user, user);

        var doAfterArgs = new DoAfterArgs(EntityManager, user, entity.Comp.PuffDelay, new SmokeRingDoAfterEvent(GetNetCoordinates(target)), entity.Owner, target: user, used: entity.Owner)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = true,
        };

        return DoAfter.TryStartDoAfter(doAfterArgs);
    }
}
