using Content.Shared._Lust.ErpStatus;

namespace Content.Shared.AlertLevel;

public sealed partial class AlertLevelPrototype
{
    /// <summary>
    ///     Правила, которые код тревоги применяет к ERP статусу персонала.
    ///     Пустой список снимает все ограничения.
    /// </summary>
    [DataField]
    public List<ErpStatusRule> ErpStatusRules = [];
}
