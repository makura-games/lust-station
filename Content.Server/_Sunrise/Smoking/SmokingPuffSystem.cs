using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared._Sunrise.Smoking;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Smoking;

/// <summary>
/// Серверная система выполнения затяжек: перенос реагентов в кровь, выделение дыма в атмосферу и спавн колечек.
/// </summary>
public sealed partial class SmokingPuffSystem : SharedSmokingPuffSystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private ReactiveSystem _reactive = default!;
    [Dependency] private SmokingSystem _smoking = default!;
    [Dependency] private SmokeRingSystem _smokeRing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmokablePuffComponent, SmokablePuffDoAfterEvent>(OnPuffDoAfter);
        SubscribeLocalEvent<SmokablePuffComponent, SmokeRingDoAfterEvent>(OnSmokeRingDoAfter);
    }

    private void OnPuffDoAfter(Entity<SmokablePuffComponent> entity, ref SmokablePuffDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (TryFinalizePuff(entity, args.Args.User, args.Args.Target.Value, args.Forced))
            args.Handled = true;
    }

    public bool TryFinalizePuff(
        Entity<SmokablePuffComponent> entity,
        EntityUid user,
        EntityUid target,
        bool forced = false)
    {
        if (!CanFinalizePuff(entity, user, target, out var smokable, out var soln, out var solution))
            return false;

        DoFinalizePuff(entity, user, target, smokable, soln.Value, solution, forced);
        return true;
    }

    public bool CanFinalizePuff(
        Entity<SmokablePuffComponent> entity,
        EntityUid user,
        EntityUid target,
        [NotNullWhen(true)] out SmokableComponent? smokable,
        [NotNullWhen(true)] out Entity<SolutionComponent>? soln,
        [NotNullWhen(true)] out Solution? solution)
    {
        smokable = null;
        soln = null;
        solution = null;

        if (!Ingestion.HasMouthAvailable(user, target))
            return false;

        if (!TryComp(entity.Owner, out smokable) || smokable.State != SmokableState.Lit)
            return false;

        if (!Solution.TryGetSolution(entity.Owner, smokable.Solution, out var solutionEnt, out var sol) ||
            sol.Volume <= FixedPoint2.Zero)
        {
            return false;
        }

        soln = solutionEnt;
        solution = sol;
        return true;
    }

    public void DoFinalizePuff(
        Entity<SmokablePuffComponent> entity,
        EntityUid user,
        EntityUid target,
        SmokableComponent smokable,
        Entity<SolutionComponent> soln,
        Solution solution,
        bool forced)
    {
        var puffVol = FixedPoint2.Min(FixedPoint2.New(entity.Comp.PuffVolume), solution.Volume);
        var split = Solution.SplitSolution(soln, puffVol);

        if (split.Volume > FixedPoint2.Zero)
        {
            _reactive.DoEntityReaction(target, split, ReactionMethod.Ingestion);
            _bloodstream.TryAddToBloodstream(target, split);
        }

        // Выделение части дыма в атмосферу
        var environment = _atmos.GetContainingMixture(target, true, true);
        if (environment != null)
        {
            var merger = new GasMixture(1) { Temperature = Atmospherics.T20C + 35f };
            merger.SetMoles(Gas.WaterVapor, 0.4f);
            _atmos.Merge(environment, merger);
        }

        if (solution.Volume <= FixedPoint2.Zero)
        {
            _smoking.SetSmokableState(entity.Owner, SmokableState.Burnt, smokable);
        }

        if (entity.Comp.ExhaleSound != null)
        {
            Audio.PlayPvs(entity.Comp.ExhaleSound, target, AudioParams.Default.WithVolume(-1.5f).WithMaxDistance(7f).WithVariation(0.04f));
        }

        if (forced)
        {
            var targetName = Identity.Entity(target, EntityManager);
            var userName = Identity.Entity(user, EntityManager);
            Popup.PopupEntity(Loc.GetString("smoking-puff-success-forced", ("user", userName)), target, target);
            Popup.PopupEntity(Loc.GetString("smoking-puff-success-user-forced", ("target", targetName)), user, user);
        }
        else
        {
            Popup.PopupEntity(Loc.GetString("smoking-puff-success"), target, target);
        }
    }

    private void OnSmokeRingDoAfter(Entity<SmokablePuffComponent> entity, ref SmokeRingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.Args.User;

        if (TryComp<SmokableComponent>(entity.Owner, out var smokable) &&
            smokable.State == SmokableState.Lit &&
            Solution.TryGetSolution(entity.Owner, smokable.Solution, out var soln, out var solution) &&
            solution.Volume > FixedPoint2.Zero)
        {
            var puffVol = FixedPoint2.Min(FixedPoint2.New(entity.Comp.PuffVolume), solution.Volume);
            var split = Solution.SplitSolution(soln.Value, puffVol);

            if (split.Volume > FixedPoint2.Zero)
            {
                _reactive.DoEntityReaction(user, split, ReactionMethod.Ingestion);
                _bloodstream.TryAddToBloodstream(user, split);
            }

            if (solution.Volume <= FixedPoint2.Zero)
            {
                _smoking.SetSmokableState(entity.Owner, SmokableState.Burnt, smokable);
            }
        }

        if (entity.Comp.ExhaleSound != null)
        {
            Audio.PlayPvs(entity.Comp.ExhaleSound, user, AudioParams.Default.WithVolume(-1.5f).WithMaxDistance(7f).WithVariation(0.04f));
        }

        Popup.PopupEntity(Loc.GetString("smoking-puff-smoke-ring", ("user", Identity.Entity(user, EntityManager))), user);

        var targetCoords = GetCoordinates(args.TargetCoordinates);
        _smokeRing.SpawnSmokeRing(user, targetCoords, Gas.WaterVapor);

        args.Handled = true;
    }
}
