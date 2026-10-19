using Content.Server._Lust.Anomaly.Systems;

namespace Content.Server._Lust.Anomaly.Components;

/// <summary>
/// Компонент для персонажей в радиусе звуковой аномалии, заставляющий их шаги и движения неестественно звенеть, скрипеть и эховать.
/// </summary>
[RegisterComponent, Access(typeof(SoundAnomalySystem))]
public sealed partial class SoundAnomalyResonanceComponent : Component
{
    /// <summary>
    /// Время последнего воспроизведенного резонансного звука шага.
    /// </summary>
    public TimeSpan LastStepTime = TimeSpan.Zero;
}
