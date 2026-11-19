using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Сбой работы системы датчиков здоровья
/// </summary>

[RegisterComponent]
public sealed partial class CrewMonitoringMalfRuleComponent : Component
{
    /// <summary>
    /// Минимальное количетсво игроков в процентах от живых
    /// </summary>

    [DataField]
    public float MinPlayerFraction = 0.1f;

    /// <summary>
    /// Максимальное количетсво игроков в процентах от живых
    /// </summary>

    [DataField]
    public float MaxPlayerFraction = 0.3f;

    /// <summary>
    /// Сколько процентов от выборки будут иметь статус "критическое"
    /// </summary>

    [DataField]
    public int CriticalChance = 25;

    /// <summary>
    /// Сколько процентов от выборки будут иметь статус "мертв"
    /// </summary>

    [DataField]
    public int DeadChance = 10;

    /// <summary>
    /// Сколько процентов будут иметь статус "плохо"
    /// </summary>

    [DataField]
    public int PoorChance = 50;

    public HashSet<EntityUid> AffectedPlayers = new();
}