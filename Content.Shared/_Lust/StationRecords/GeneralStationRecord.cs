using Content.Shared._Lust.Preferences;

#pragma warning disable IDE0130
namespace Content.Shared.StationRecords;

/// <summary>
/// Расширение GeneralStationRecord для Санитизация портфолио
/// </summary>
public sealed partial record GeneralStationRecord
{
    /// <summary>
    /// Санитизация портфолио: лимиты длин накладывает PortfolioProfile.EnsureValid.
    /// Профиль, которого нет, остаётся null.
    /// </summary>
    private static GeneralStationRecord SanitizeLustProfile(GeneralStationRecord original)
    {
        if (original.HumanoidProfile is null)
            return original;

        var portfolio = new PortfolioProfile(original.HumanoidProfile.Portfolio);
        portfolio.EnsureValid();

        return original with
        {
            HumanoidProfile = original.HumanoidProfile.WithPortfolio(portfolio),
        };
    }
}
