using Content.Shared._Lust.Smell.Components;
using Content.Shared.Verbs;

namespace Content.Shared._Lust.Smell;

/// <summary>
/// The system for launching the verb “smell”
/// <see cref="SmellExecuteEvent"/>.
/// </summary>
public sealed partial class SharedSmellSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScentComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }

    /// <summary>
    /// Adds the "smell" verb to a target with ScentComponent if the smeller is
    /// itself capable of smelling (has SmellComponent) and passes access/interaction checks.
    /// </summary>
    private void OnGetInteractionVerbs(
        Entity<ScentComponent> target,
        ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!HasComp<SmellComponent>(args.User))
            return;

        var user = args.User;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("smell-verb"),
            TextStyleClass = "Default",
            EventTarget = target.Owner,
            ExecutionEventArgs = new SmellExecuteEvent(user),
        });
    }
}
