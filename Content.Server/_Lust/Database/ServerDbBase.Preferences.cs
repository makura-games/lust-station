using Content.Shared.Preferences;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    private static void StoreLustProfileData(Profile profile, HumanoidCharacterProfile humanoid)
    {
        profile.ErpData ??= new ProfileErp
        {
            ProfileId = profile.Id,
            Profile = profile,
        };

        profile.ErpData.Erp = humanoid.Erp.ToString();
        profile.ErpData.Virginity = humanoid.Virginity.ToString();
        profile.ErpData.AnalVirginity = humanoid.AnalVirginity.ToString();
    }
}
