using Content.Server.Database;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Preferences.Managers;

public sealed partial class ServerPreferencesManager
{
    private static HumanoidCharacterProfile ApplyLustProfileData(
        HumanoidCharacterProfile humanoid,
        Profile profile)
    {
        var erp = Erp.Ask;
        var virginity = Virginity.No;
        var analVirginity = Virginity.Yes;

        if (profile.ErpData is { } erpData)
        {
            if (Enum.TryParse<Erp>(erpData.Erp, true, out var parsedErp))
                erp = parsedErp;

            if (Enum.TryParse<Virginity>(erpData.Virginity, true, out var parsedVirginity))
                virginity = parsedVirginity;

            if (Enum.TryParse<Virginity>(erpData.AnalVirginity, true, out var parsedAnalVirginity))
                analVirginity = parsedAnalVirginity;
        }

        return humanoid
            .WithErp(erp)
            .WithVirginity(virginity)
            .WithAnalVirginity(analVirginity);
    }
}
