using Content.Server._Lust.Anomaly.Systems;

namespace Content.Server._Lust.Anomaly.Components;

/// <summary>
/// Компонент для активного или инертного ядра звуковой аномалии. При использовании в руке воспроизводит сохраненные фразы.
/// </summary>
[RegisterComponent, Access(typeof(SoundAnomalySystem))]
public sealed partial class SoundAnomalyCoreComponent : Component
{
    /// <summary>
    /// Является ли ядро инертным (погасшим). Инертное ядро говорит очень тихо и с шипящими помехами.
    /// </summary>
    [DataField]
    public bool IsInert = false;

    /// <summary>
    /// Перезарядка между использованиями в руке.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Внутреннее время следующего допустимого использования.
    /// </summary>
    public TimeSpan NextUseTime = TimeSpan.Zero;

    /// <summary>
    /// Сохраненные записи фраз для повторения.
    /// </summary>
    public List<HeardSpeechRecord> SavedRecords = new();
}
