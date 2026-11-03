using Content.Shared.Humanoid;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void InitializeLustProfileEditor()
    {
        ErpButton.OnItemSelected += args =>
        {
            ErpButton.SelectId(args.Id);
            SetErp((Erp) args.Id);
        };

        VirginityButton.OnItemSelected += args =>
        {
            VirginityButton.SelectId(args.Id);
            SetVirginity((Virginity) args.Id);
        };

        AnalVirginityButton.OnItemSelected += args =>
        {
            AnalVirginityButton.SelectId(args.Id);
            SetAnalVirginity((Virginity) args.Id);
        };
    }

    private void UpdateLustControls()
    {
        if (Profile is null)
            return;

        ErpButton.Clear();
        foreach (var erp in Enum.GetValues<Erp>())
        {
            ErpButton.AddItem(
                Loc.GetString($"humanoid-profile-editor-erp-{erp.ToString().ToLowerInvariant()}-text"),
                (int) erp);
        }
        ErpButton.SelectId(Enum.IsDefined(Profile.Erp) ? (int) Profile.Erp : (int) Erp.No);

        VirginityButton.Clear();
        AnalVirginityButton.Clear();
        foreach (var virginity in Enum.GetValues<Virginity>())
        {
            VirginityButton.AddItem(
                Loc.GetString($"humanoid-profile-editor-virginity-{virginity.ToString().ToLowerInvariant()}-text"),
                (int) virginity);
            AnalVirginityButton.AddItem(
                Loc.GetString($"humanoid-profile-editor-anal-virginity-{virginity.ToString().ToLowerInvariant()}-text"),
                (int) virginity);
        }

        VirginityButton.SelectId(Enum.IsDefined(Profile.Virginity) ? (int) Profile.Virginity : (int) Virginity.Yes);
        AnalVirginityButton.SelectId(Enum.IsDefined(Profile.AnalVirginity)
            ? (int) Profile.AnalVirginity
            : (int) Virginity.Yes);
    }

    private void SetErp(Erp erp)
    {
        Profile = Profile?.WithErp(erp);
        SetDirty();
    }

    private void SetVirginity(Virginity virginity)
    {
        Profile = Profile?.WithVirginity(virginity);
        SetDirty();
    }

    private void SetAnalVirginity(Virginity virginity)
    {
        Profile = Profile?.WithAnalVirginity(virginity);
        SetDirty();
    }
}
