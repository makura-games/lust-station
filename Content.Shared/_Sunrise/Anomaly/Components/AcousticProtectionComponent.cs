using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Anomaly.Components;

/// <summary>
/// Компонент для экипировки (наушники, каски, гермошлемы скафандров),
/// защищающий владельца от акустических аномалий: блокирует кражу голоса TTS,
/// галлюцинации и контузии.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AcousticProtectionComponent : Component
{
    /// <summary>
    /// Блокирует ли этот предмет кражу и подмену голоса TTS.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ProtectsTts = true;

    /// <summary>
    /// Блокирует ли слуховые галлюцинации.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ProtectsHallucinations = true;

    /// <summary>
    /// Снижает ли силу оглушения и дезориентации от звукового удара.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DampensStun = true;
}
