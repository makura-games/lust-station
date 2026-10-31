using Content.Server._Lust.Anomaly.Systems;

namespace Content.Server._Lust.Anomaly.Components;

/// <summary>
/// Компонент для персональных слуховых галлюцинаций (шепот TTS и тихие фантомные звуки в голове) после звукового импульса аномалии.
/// </summary>
[RegisterComponent, Access(typeof(SoundAnomalySystem))]
public sealed partial class SoundAnomalyHauntedComponent : Component
{
    /// <summary>
    /// Время окончания действия слуховых галлюцинаций.
    /// </summary>
    public TimeSpan EndTime = TimeSpan.Zero;

    /// <summary>
    /// Время следующего проявления галлюцинации в голове.
    /// </summary>
    public TimeSpan NextHallucinationTime = TimeSpan.Zero;

    /// <summary>
    /// Минимальный интервал между галлюцинациями.
    /// </summary>
    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Максимальный интервал между галлюцинациями.
    /// </summary>
    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromSeconds(9);

    /// <summary>
    /// Сущность аномалии-источника, из которой берутся фразы и звуки.
    /// </summary>
    public EntityUid? SourceAnomaly;

    /// <summary>
    /// Сохраненные фразы для галлюцинаций на случай, если аномалия-источник будет удалена.
    /// </summary>
    public List<HeardSpeechRecord> SavedRecords = new();

    /// <summary>
    /// Исходный голос жертвы до искажения чужим TTS.
    /// </summary>
    public string? OriginalVoiceId;

    /// <summary>
    /// Время возврата исходного голоса (по умолчанию через 5 минут).
    /// </summary>
    public TimeSpan VoiceResetTime = TimeSpan.Zero;
}
