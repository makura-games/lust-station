using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Случайные вендинговые автоматы выбрасывают компоненты
/// </summary>

[RegisterComponent, Access(typeof(VendingMachineMalfRuleSystem))]
public sealed partial class VendingMachineMalfRuleComponent : Component
{
    /// <summary>
    /// Минимальное количество машин ивента
    /// </summary>

    [DataField]
    public int MinMachines = 1;

    /// <summary>
    /// Максимальное количество машин ивента
    /// </summary>
    [DataField]
    public int MaxMachines = 20;

    /// <summary>
    /// Минимальное количество предметов которые выбросит автомат 
    /// </summary>
    [DataField]
    public int MinItems = 1;

    /// <summary>
    /// Максимальное количество предметов которые выбросит автомат 
    /// </summary>

    [DataField]
    public int MaxItems = 7;
}