using Content.Shared._Lust.Smell;
using Content.Shared._Lust.Smell.Components;
using Content.Shared._Lust.Smell.Prototypes;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Lust.Smell;

public sealed class SmellSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ScentComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }


    private void OnGetInteractionVerbs(
        Entity<ScentComponent> target,
        ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<SmellComponent>(args.User, out SmellComponent? smell)
            || smell.SmellBlocked)
        {
            return;
        }
        EntityUid user = args.User;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("smell-verb"),
            TextStyleClass = "Default",
            Act = () => TrySmell(user, target)
        });
    }

    public sealed record PersonalCharacteristics
    {
        public int Age { get; init; }
        public Gender Gender { get; init; }
        public string? Voice { get; init; }
    }
    public bool TrySmell(EntityUid user, Entity<ScentComponent> target)
    {
        if (!CanSmell(user, target))
            return false;

        DoSmell(user, target);
        return true;
    }

    public bool CanSmell(EntityUid user, Entity<ScentComponent> target)
    {
        if (!TryComp<SmellComponent>(user, out SmellComponent? smell)
            || smell.SmellBlocked)
        {
            return false;
        }

        if (!_actionBlocker.CanInteract(user, target))
            return false;

        return _interaction.InRangeUnobstructed(user, target.Owner);
    }

    private void DoSmell(EntityUid user, Entity<ScentComponent> target)
    {
        FormattedMessage message = new();
        List<string> staticNotes = [];

        foreach (ProtoId<ScentPrototype> scentId in target.Comp.BaseScents)
        {
            ScentPrototype scent = _prototypes.Index<ScentPrototype>(scentId);
            staticNotes.Add(Loc.GetString(scent.Description));
        }

        ScentSignature? signature = GetPersonalSignature(target);

        if (staticNotes.Count > 0)
        {
            message.AddMarkupOrThrow(Loc.GetString(
                "smell-result-static",
                ("notes", string.Join(", ", staticNotes))));
        }

        if (signature != null)
        {
            if (staticNotes.Count > 0)
                message.AddMarkupOrThrow("\n");

            List<string> personalNotes = [];

            foreach (LocId note in signature.Notes)
            {
                personalNotes.Add(Loc.GetString(note));
            }

            message.AddMarkupOrThrow(Loc.GetString(
                "smell-result-personal",
                ("color", signature.Color.ToHex()),
                ("notes", string.Join(", ", personalNotes))));
        }

        if (staticNotes.Count == 0 && signature == null)
        {
            message.AddMarkupOrThrow(Loc.GetString("smell-result-none"));
        }

        _examine.SendExamineTooltip(user, target, message, false, false);
    }

    private ScentSignature? GetPersonalSignature(Entity<ScentComponent> target)
    {
        if (target.Comp.PersonalScentProfile is not { } profileId)
            return null;


        PersonalScentProfilePrototype profile =
            _prototypes.Index<PersonalScentProfilePrototype>(profileId);

        string name = Name(target.Owner) ?? "unknown";

        PersonalCharacteristics? characteristics = null;

        if (TryComp<HumanoidAppearanceComponent>(target.Owner, out HumanoidAppearanceComponent? appearanceComponent))
        {
            characteristics = new PersonalCharacteristics
            {
                Age = appearanceComponent.Age,
                Gender = appearanceComponent.Gender,
                Voice = appearanceComponent.Voice,
            };
        }


        string seed = $"{name}";
        if (characteristics != null)
        {
            seed += $":{characteristics.Age}:{characteristics.Gender}:{characteristics.Voice}";
        }

        return ScentSignatureGenerator.Generate(seed, profile);
    }

}
