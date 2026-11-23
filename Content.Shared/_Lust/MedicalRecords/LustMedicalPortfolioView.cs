using Content.Shared.Preferences;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.MedicalRecords;

/// <summary>
/// Поля портфолио, которые нужны медицинской консоли.
/// </summary>
[Serializable, NetSerializable]
public sealed record LustMedicalPortfolioView(
    string? CloseRelatives,
    string? EmergencyContact,
    string? PhysiologicalTraits,
    string? PsychologicalTraits)
{
    /// <summary>
    /// Собирает модель из профиля пользователя. null, если профиля или портфолио нет.
    /// </summary>
    public static LustMedicalPortfolioView? FromProfile(HumanoidCharacterProfile? profile)
    {
        var portfolio = profile?.Portfolio;
        if (portfolio is null)
            return null;

        return new LustMedicalPortfolioView(
            portfolio.CloseRelatives,
            portfolio.EmergencyContact,
            portfolio.PhysiologicalTraits,
            portfolio.PsychologicalTraits);
    }
}
