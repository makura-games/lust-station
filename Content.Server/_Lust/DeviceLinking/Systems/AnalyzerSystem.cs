using Content.Shared.Analyzer;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Content.Shared.Interaction;

namespace Content.Shared.Analyzer;

public sealed partial class AnalyzerSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnalyzerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<AnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnUseInHand(Entity<AnalyzerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (string.IsNullOrEmpty(ent.Comp.CurrentTargetComponent))
        {
            GiveNewTask(ent, args.User);
            return;
        }

        ShowCurrentTask(ent, args.User);
    }

    private void OnAfterInteract(Entity<AnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (string.IsNullOrEmpty(ent.Comp.CurrentTargetComponent))
            return;

        args.Handled = true;

        var target = args.Target.Value;

        if (!HasComponentByName(target, ent.Comp.CurrentTargetComponent))
        {
            _popup.PopupEntity(Loc.GetString("analyzer-wrong-target"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        _popup.PopupEntity(Loc.GetString("analyzer-task-complete"), ent, args.User, PopupType.Large);

        var pointsEv = new AnalyzerAddPointsEvent(ent, ent.Comp.PointsPerScan);
        RaiseLocalEvent(ent, ref pointsEv);

        ent.Comp.CurrentTargetComponent = null;
        Dirty(ent);
        GiveNewTask(ent, args.User);
    }

    private void GiveNewTask(Entity<AnalyzerComponent> ent, EntityUid user)
    {
        if (ent.Comp.PossibleComponents.Count == 0)
            return;

        var component = _random.Pick(ent.Comp.PossibleComponents);
        ent.Comp.CurrentTargetComponent = component;
        Dirty(ent);

        var locId = $"analyzer-task-{component.ToLower()}";
        var message = Loc.GetString(locId);
        _popup.PopupEntity(message, ent, user, PopupType.Medium);
    }

    private void ShowCurrentTask(Entity<AnalyzerComponent> ent, EntityUid user)
    {
        if (string.IsNullOrEmpty(ent.Comp.CurrentTargetComponent))
            return;

        var locId = $"analyzer-task-{ent.Comp.CurrentTargetComponent.ToLower()}";
        var message = Loc.GetString(locId);
        _popup.PopupEntity(message, ent, user, PopupType.Medium);
    }

    private bool HasComponentByName(EntityUid uid, string componentName)
    {
        if (!_componentFactory.TryGetRegistration(componentName, out var registration))
        {
            if (!_componentFactory.TryGetRegistration(componentName + "Component", out registration))
                return false;
        }

        return HasComp(uid, registration.Type);
    }
}