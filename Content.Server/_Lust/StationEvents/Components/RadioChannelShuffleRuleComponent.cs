using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Сбой работы системы шифрования радиособщений - перемешивание
/// </summary>

[RegisterComponent]
public sealed partial class RadioChannelShuffleRuleComponent : Component
{
    
    [DataField]
    public float RedirectChance = 0.3f;

    // Только эти каналы участвуют в событии Каналы вне списка не затрагиваются ни как источник, ни как назначение.
    [DataField]
    public List<string> Channels = new()
    {
        "Common",
        "Command",
        "Engineering",
        "Medical",
        "Security",
        "Science",
        "Service",
        "Supply",
    };

    // Станция, выбранная при запуске события.
    public EntityUid? Station;
}