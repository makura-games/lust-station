using Content.Shared._Lust.Borgs.Components;
using Content.Shared.Humanoid;
using Content.Shared.Popups;

namespace Content.Shared._Lust.Borgs;

public sealed partial class BorgSexSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSexComponent, BorgCycleSexActionEvent>(OnCycleSex);
    }

    private void OnCycleSex(Entity<BorgSexComponent> ent, ref BorgCycleSexActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        var nextSex = ent.Comp.Sex switch
        {
            Sex.Unsexed => Sex.Male,
            Sex.Male => Sex.Female,
            Sex.Female => Sex.Futanari,
            _ => Sex.Unsexed,
        };

        if (TrySetSex(ent, nextSex))
            args.Handled = true;
    }

    public bool TrySetSex(Entity<BorgSexComponent> ent, Sex sex)
    {
        if (!CanSetSex(ent, sex))
            return false;

        ent.Comp.Sex = sex;
        Dirty(ent);

        var changed = new BorgSexChangedEvent();
        RaiseLocalEvent(ent.Owner, ref changed);

        _popup.PopupPredicted(Loc.GetString($"borg-sex-selected-{sex.ToString().ToLowerInvariant()}"), ent, ent);
        return true;
    }

    public bool CanSetSex(Entity<BorgSexComponent> ent, Sex sex)
    {
        return (sex is Sex.Male or Sex.Female or Sex.Futanari or Sex.Unsexed) && ent.Comp.Sex != sex;
    }

    public static bool TryGetSex(EntityUid uid, IEntityManager entityManager, out Sex sex)
    {
        if (entityManager.TryGetComponent<BorgSexComponent>(uid, out var borgSex))
        {
            sex = borgSex.Sex;
            return true;
        }

        if (entityManager.TryGetComponent<HumanoidProfileComponent>(uid, out var humanoid))
        {
            sex = humanoid.Sex;
            return true;
        }

        sex = Sex.Unsexed;
        return false;
    }
}
