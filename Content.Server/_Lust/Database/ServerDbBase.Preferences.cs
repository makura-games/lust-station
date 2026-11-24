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

        // Портфолио
        profile.PortfolioData ??= new ProfilePortfolio
        {
            ProfileId = profile.Id,
            Profile = profile,
        };

        var pd = profile.PortfolioData;
        pd.DistinguishingFeatures = humanoid.Portfolio.DistinguishingFeatures;
        pd.Region = humanoid.Portfolio.Region;
        pd.PlanetOrColony = humanoid.Portfolio.PlanetOrColony;
        pd.Address = humanoid.Portfolio.Address;
        pd.Education = humanoid.Portfolio.Education;
        pd.WorkExperience = humanoid.Portfolio.WorkExperience;
        pd.MaritalStatusValue = humanoid.Portfolio.MaritalStatusValue;
        pd.CloseRelatives = humanoid.Portfolio.CloseRelatives;
        pd.EmergencyContact = humanoid.Portfolio.EmergencyContact;
        pd.PhysiologicalTraits = humanoid.Portfolio.PhysiologicalTraits;
        pd.PsychologicalTraits = humanoid.Portfolio.PsychologicalTraits;
        pd.ArrestHistory = humanoid.Portfolio.ArrestHistory;
        pd.ConvictionHistory = humanoid.Portfolio.ConvictionHistory;
    }
}
