using Content.Server._Sunrise.Anomaly.Systems;

namespace Content.Server._Sunrise.Anomaly.Components;

/// <summary>
/// Маркерный компонент для раций, интеркомов и гарнитур, поврежденных звуковым импульсом аномалии.
/// </summary>
[RegisterComponent, Access(typeof(SoundAnomalySystem))]
public sealed partial class SoundAnomalyOverloadedRadioComponent : Component
{
    /// <summary>
    /// Время, до которого устройство выведено из строя перегрузкой.
    /// </summary>
    [DataField]
    public TimeSpan OverloadedUntil = TimeSpan.Zero;
}
