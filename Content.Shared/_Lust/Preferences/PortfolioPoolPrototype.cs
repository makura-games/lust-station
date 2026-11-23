using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Preferences;

/// <summary>
/// Прототип пулов генерации для полей партфолио
/// </summary>
[Prototype]
public sealed partial class PortfolioPoolPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Ключи локализации общих вариантов. Используются, если расового списка нет или он не выпал.
    /// </summary>
    [DataField(required: true)]
    public List<LocId> General = new();

    /// <summary>
    /// Расовые варианты: ключ — ID вида, значение — ключи локализации.
    /// </summary>
    [DataField]
    public Dictionary<string, List<LocId>> Species = new();
}
