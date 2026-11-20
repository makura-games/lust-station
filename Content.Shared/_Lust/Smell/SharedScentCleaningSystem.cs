using Content.Shared._Lust.Smell.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Lust.Smell;

/// <summary>
/// The system for launching the verb “wash scents”
/// <see cref="ScentCleaningExecuteEvent"/>.
/// </summary>
public sealed partial class SharedScentCleaningSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScentCleaningComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
    }

    /// <summary>
    /// Right-click with soap in hand: show the "wash scents" verb, but only
    /// if the target is a scent bearer (has ScentComponent).
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
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/bubbles.svg.192dpi.png")),
            Text = Loc.GetString("scent-cleaning-verb-text"),
            Message = Loc.GetString("scent-cleaning-verb-message"),
            DoContactInteraction = false,
            EventTarget = cleaner.Owner,
            ExecutionEventArgs = new ScentCleaningExecuteEvent(user, target),
        });
    }
}
