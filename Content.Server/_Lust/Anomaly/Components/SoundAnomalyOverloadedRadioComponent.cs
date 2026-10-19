using Content.Server._Lust.Anomaly.Systems;

namespace Content.Server._Lust.Anomaly.Components;

/// <summary>
/// Маркерный компонент для раций, интеркомов и гарнитур, поврежденных звуковым импульсом аномалии.
/// </summary>
[RegisterComponent, Access(typeof(SoundAnomalySystem))]
public sealed partial class SoundAnomalyOverloadedRadioComponent : Component
{
    /// <summary>
    /// Время, до которого устройство выведено из строя перегрузкой.
    /// </summary>
    public TimeSpan OverloadedUntil = TimeSpan.Zero;
}
