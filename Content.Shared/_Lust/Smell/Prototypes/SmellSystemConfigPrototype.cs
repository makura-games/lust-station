using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Prototypes;

/// <summary>
/// Единый конфиг появления и длительности временных запахов от источников.
/// Хранит пороги урона и длительности запахов ран, яда, чужой крови,
/// возбуждения и оргазма — чтобы балансировать без пересборки.
/// </summary>
[Prototype("smellSystemConfig")]
public sealed partial class SmellSystemConfigPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Порог накопленного урона (порезов/ушибов), с которого существо
    /// начинает пахнуть собственной кровью или синяками.
    /// </summary>
    [DataField]
    public int WoundScentThreshold { get; private set; } = 10;

    /// <summary>
    /// Порог накопленного яда (Poison), с которого тело пахнет токсинами.
    /// </summary>
    [DataField]
    public int PoisonScentThreshold { get; private set; } = 25;

    /// <summary>
    /// Длительность запаха от раны или ушиба.
    /// </summary>
    [DataField]
    public TimeSpan WoundScentDuration { get; private set; } = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Длительность запаха от отравления.
    /// </summary>
    [DataField]
    public TimeSpan PoisonScentDuration { get; private set; } = TimeSpan.FromSeconds(200);

    /// <summary>
    /// Длительность запаха чужой крови при добивании жертвы.
    /// </summary>
    [DataField]
    public TimeSpan OtherBloodScentDuration { get; private set; } = TimeSpan.FromSeconds(600);

    /// <summary>
    /// Длительность запаха возбуждения (на себе).
    /// </summary>
    [DataField]
    public TimeSpan ArousalScentDuration { get; private set; } = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Длительность запаха оргазма (на себе и на партнёре).
    /// </summary>
    [DataField]
    public TimeSpan OrgasmScentDuration { get; private set; } = TimeSpan.FromSeconds(500);

    /// <summary>
    /// Дальность (в метрах), в пределах которой можно смыть запах с цели.
    /// </summary>
    [DataField]
    public float ScentCleaningRange { get; private set; } = 1.5f;

    /// <summary>
    /// Цвет текста, когда основной запах цели скрыт маскировкой (после мытья мылом).
    /// </summary>
    [DataField]
    public Color MaskedScentColor { get; private set; } = Color.FromHex("#a6d8ff");
}