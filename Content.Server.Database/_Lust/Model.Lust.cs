using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Content.Shared._Lust.Preferences;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public partial class Profile
{
    public ProfilePortfolio? PortfolioData { get; set; }
}

public class ProfilePortfolio
{
    public int Id { get; set; }
    public int ProfileId { get; set; }

    public Profile Profile { get; set; } = null!;

    [Required, MaxLength(LustPorftolioLimits.PortfolioText)]
    public string DistinguishingFeatures { get; set; } = string.Empty;

    [Required, MaxLength(LustPorftolioLimits.PortfolioShortText)]
    public string Region { get; set; } = string.Empty;

    [Required, MaxLength(LustPorftolioLimits.PortfolioShortText)]
    public string PlanetOrColony { get; set; } = string.Empty;

    [Required, MaxLength(LustPorftolioLimits.PortfolioShortText)]
    public string Address { get; set; } = string.Empty;

    [Required, MaxLength(LustPorftolioLimits.PortfolioText)]
    public string Education { get; set; } = string.Empty;

    [Required, MaxLength(LustPorftolioLimits.PortfolioText)]
    public string WorkExperience { get; set; } = string.Empty;

    public MaritalStatus MaritalStatusValue { get; set; }

    [Required, MaxLength(LustPorftolioLimits.PortfolioText)]
    public string CloseRelatives { get; set; } = string.Empty;

    [Required, MaxLength(LustPorftolioLimits.PortfolioText)]
    public string EmergencyContact { get; set; } = string.Empty;

    [Required, MaxLength(LustPorftolioLimits.PortfolioText)]
    public string PhysiologicalTraits { get; set; } = string.Empty;

    [Required, MaxLength(LustPorftolioLimits.PortfolioText)]
    public string PsychologicalTraits { get; set; } = string.Empty;

    [Required, MaxLength(LustPorftolioLimits.PortfolioText)]
    public string ArrestHistory { get; set; } = string.Empty;

    [Required, MaxLength(LustPorftolioLimits.PortfolioText)]
    public string ConvictionHistory { get; set; } = string.Empty;
}
