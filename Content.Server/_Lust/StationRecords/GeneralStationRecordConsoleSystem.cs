using Content.Shared._Lust.Preferences;
using Content.Shared.StationRecords;
using Robust.Shared.Utility;

#pragma warning disable IDE0130
namespace Content.Server.StationRecords;

/// <summary>
/// Расширение системы печатанья информаци в концоли станционного учёта о человеке
/// </summary>
public sealed partial class GeneralStationRecordConsoleSystem
{
    /// <summary>
    /// Приминяем измменение только мед данных в общем records
    /// </summary>
    internal GeneralStationRecord PreserveMedicalPortfolio(
        GeneralStationRecord incoming,
        GeneralStationRecord stored)
    {
        var incomingProfile = incoming.HumanoidProfile;
        var storedPortfolio = stored.HumanoidProfile?.Portfolio;
        if (incomingProfile is null || storedPortfolio is null)
            return incoming;

        var portfolio = new PortfolioProfile(incomingProfile.Portfolio)
        {
            PhysiologicalTraits = storedPortfolio.PhysiologicalTraits,
            PsychologicalTraits = storedPortfolio.PsychologicalTraits,
        };

        return incoming with { HumanoidProfile = incomingProfile.WithPortfolio(portfolio) };
    }

    private string PrintableField(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Loc.GetString("printed-station-records-unrecognized")
            : FormattedMessage.EscapeText(value);
    }

    private string PrintableMaritalStatus(MaritalStatus? maritalStatus)
    {
        if (maritalStatus is null)
            return Loc.GetString("printed-station-records-unrecognized");

        return Loc.GetString(
            $"lust-humanoid-profile-editor-portfolio-marital-{maritalStatus.Value.ToString().ToLowerInvariant()}-text");
    }
}
