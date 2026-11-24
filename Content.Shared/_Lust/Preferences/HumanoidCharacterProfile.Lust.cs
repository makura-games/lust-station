using Content.Shared._Lust.Preferences;
using Content.Shared.Humanoid;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    [DataField]
    public Erp Erp { get; set; } = Erp.Ask;

    [DataField]
    public Virginity Virginity { get; set; } = Virginity.No;

    [DataField]
    public Virginity AnalVirginity { get; set; } = Virginity.Yes;

    [DataField]
    public PortfolioProfile Portfolio { get; set; } = new();

    public HumanoidCharacterProfile WithErp(Erp erp)
    {
        return new(this) { Erp = erp };
    }

    public HumanoidCharacterProfile WithVirginity(Virginity virginity)
    {
        return new(this) { Virginity = virginity };
    }

    public HumanoidCharacterProfile WithAnalVirginity(Virginity analVirginity)
    {
        return new(this) { AnalVirginity = analVirginity };
    }

    public HumanoidCharacterProfile WithPortfolio(PortfolioProfile portfolio)
    {
        return new(this) { Portfolio = new(portfolio) };
    }

    private void CopyLustProfile(HumanoidCharacterProfile other)
    {
        Erp = other.Erp;
        Virginity = other.Virginity;
        AnalVirginity = other.AnalVirginity;
        // Портфоли
        Portfolio = new(other.Portfolio);
    }

    private bool LustProfileEquals(HumanoidCharacterProfile other)
    {
        return Erp == other.Erp &&
               Virginity == other.Virginity &&
               AnalVirginity == other.AnalVirginity &&
               // Парфтоли
               Portfolio.Equals(other.Portfolio);
    }

    private void AddLustHashCode(ref HashCode hashCode)
    {
        hashCode.Add((int) Erp);
        hashCode.Add((int) Virginity);
        hashCode.Add((int) AnalVirginity);
        // Портфоли
        hashCode.Add(Portfolio);
    }
}
