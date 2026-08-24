using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._Lust.Smell;
using Content.Shared._Lust.Smell.Components;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Lust.Smell;

/// <summary>
/// Механика «мытья запахов» предметом с ScentCleaningComponent (мыло):
/// работает как верб «Смыть запах» по ПКМ на носителе запахов, DoAfter и по его завершении —
/// смыв временных запахов и временная маскировка основного запаха цели.
/// </summary>
public sealed class ScentCleaningSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SmellPrototypeCacheSystem _smellCache = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ScentCleaningComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        SubscribeLocalEvent<ScentCleaningComponent, ScentCleaningDoAfterEvent>(OnScentCleaningDoAfter);
    }

    /// <summary>
    /// ПКМ по цели с мылом в руках: показываем верб «Смыть запах», но только
    /// если цель является носителем запахов (есть ScentComponent).
    /// </summary>
    private void OnUtilityVerb(Entity<ScentCleaningComponent> cleaner, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!HasComp<ScentComponent>(args.Target))
            return;

        var user = args.User;
        var target = args.Target;

        args.Verbs.Add(new UtilityVerb
        {
            Act = () => TryCleanScents(cleaner, user, target),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/bubbles.svg.192dpi.png")),
            Text = Loc.GetString("scent-cleaning-verb-text"),
            Message = Loc.GetString("scent-cleaning-verb-message"),
            DoContactInteraction = false,
        });
    }

    /// <summary>
    /// Запускает действие по смыву запахов: проверка, попап, старт DoAfter.
    /// </summary>
    private bool TryCleanScents(Entity<ScentCleaningComponent> cleaner, EntityUid user, EntityUid target)
    {
        _popupSystem.PopupEntity(
            Loc.GetString("scent-cleaning-start", ("target", target)),
            user, user);

        var delay = cleaner.Comp.CleanDelay;
        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, new ScentCleaningDoAfterEvent(), cleaner, target: target, used: cleaner)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = _smellCache.Config.ScentCleaningRange,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        return true;
    }

    /// <summary>
    /// DoAfter завершён: смываем временные запахи цели и ставим временную маску
    /// основного запаха. Событие направляется на очиститель (EventTarget).
    /// </summary>
    private void OnScentCleaningDoAfter(EntityUid uid, ScentCleaningComponent component, ScentCleaningDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        if (!TryComp<ScentComponent>(args.Args.Target, out var scentComp))
            return;

        scentComp.TemporaryScents.Clear();
        scentComp.Masked = true;
        scentComp.MaskUntil = _timing.CurTime + component.MaskDuration;
    }
}
