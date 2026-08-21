namespace Content.Shared._Lust.Smell.Components;

/// <summary>
/// Маркер-«очиститель запахов»: предмет (мыло, спрей и т.п.), с помощью которого
/// игрок может смыть временные запахи и поставить временную маскировку основного
/// запаха цели. Верб и DoAfter реализованы в ScentCleaningSystem — всё в _Lust,
/// без вторжения в оффовскую механику ForensicsSystem.
/// </summary>
[RegisterComponent]
public sealed partial class ScentCleaningComponent : Component
{
    /// <summary>
    /// Длительность «мытья» в секундах (DoAfter). Действие прерывается
    /// движением или получением урона.
    /// </summary>
    [DataField]
    public float CleanDelay = 10.0f;

    /// <summary>
    /// Длительность временной маски основного запаха после мытья.
    /// </summary>
    [DataField]
    public TimeSpan MaskDuration = TimeSpan.FromMinutes(5);
}