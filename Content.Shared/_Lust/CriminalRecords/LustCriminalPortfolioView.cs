using Content.Shared._Lust.Preferences;
using Content.Shared.Preferences;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.CriminalRecords;

/// <summary>
/// Поля портфолио, которые нужны криминальной консоли.
/// /// </summary>
[Serializable, NetSerializable]
public sealed record LustCriminalPortfolioView(
    string? DistinguishingFeatures,
    string? Region,
    string? PlanetOrColony,
    string? Address,
    MaritalStatus? MaritalStatus,
    string? CloseRelatives,
    string? EmergencyContact,
    string? ArrestHistory,
    string? ConvictionHistory)
{
    /// <summary>
    /// Собирает модель из профиля пользователя. null, если профиля или портфолио нет.
    /// </summary>
    public static LustCriminalPortfolioView? FromProfile(HumanoidCharacterProfile? profile)
    {
        var portfolio = profile?.Portfolio;
        if (portfolio is null)
            return null;

        return new LustCriminalPortfolioView(
            portfolio.DistinguishingFeatures,
            portfolio.Region,
            portfolio.PlanetOrColony,
            portfolio.Address,
            portfolio.MaritalStatusValue,
            portfolio.CloseRelatives,
            portfolio.EmergencyContact,
            portfolio.ArrestHistory,
            portfolio.ConvictionHistory);
    }
}
