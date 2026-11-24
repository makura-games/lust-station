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
    /// Полная подготовка записи перед сохранением общей формы:
    /// сохранение медполей, санитизация и возврат null для отсутствующих.
    /// </summary>
    internal GeneralStationRecord PrepareRecordForSave(
        GeneralStationRecord incoming,
        GeneralStationRecord stored)
    {
        var preserved = PreserveMedicalPortfolio(incoming, stored);
        var sanitized = GeneralStationRecord.SanitizeRecord(preserved, in ProtoMan);

        return sanitized with
        {
            Fingerprint = string.IsNullOrWhiteSpace(incoming.Fingerprint) ? null : sanitized.Fingerprint,
            DNA = string.IsNullOrWhiteSpace(incoming.DNA) ? null : sanitized.DNA,
        };
    }

    /// <summary>
    /// Приминяем измменение только мед данных в общем records
    /// </summary>
    internal GeneralStationRecord PreserveMedicalPortfolio(
        GeneralStationRecord incoming,
        GeneralStationRecord stored)
    {

        var baseProfile = incoming.HumanoidProfile ?? stored.HumanoidProfile;
        if (baseProfile is null)
            return incoming;

        var storedPortfolio = stored.HumanoidProfile?.Portfolio ?? new PortfolioProfile();
        var portfolio = new PortfolioProfile(baseProfile.Portfolio)
        {
            PhysiologicalTraits = storedPortfolio.PhysiologicalTraits,
            PsychologicalTraits = storedPortfolio.PsychologicalTraits,
        };

        return incoming with { HumanoidProfile = baseProfile.WithPortfolio(portfolio) };
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
