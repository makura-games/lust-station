using Content.Shared._Lust.Preferences;
using Robust.Shared.Utility;

#pragma warning disable IDE0130
namespace Content.Server.StationRecords;

/// <summary>
/// Расширение системы печатанья информаци в концоли станционного учёта о человеке
/// </summary>
public sealed partial class GeneralStationRecordConsoleSystem
{

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
