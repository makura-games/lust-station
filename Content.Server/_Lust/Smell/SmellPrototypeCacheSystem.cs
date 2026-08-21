using System.Linq;
using Content.Shared._Lust.Smell.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.Smell;

/// <summary>
/// Общий кэш прототипов системы запахов статус-эффектов, созданный, чтобы один раз собрать список и перебирать
/// его при создании запахов, основанных на статус-эффектах цели (наркотики, стимуляторы, алкоголь).
/// Также ссылка на конфиг, в котором хранятся основные константы.
/// </summary>
public sealed class SmellPrototypeCacheSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    /// <summary>
    /// Список статус-эффект->запах из YAML (statusScent).
    /// </summary>
    private List<StatusScentPrototype> _statusScentProtos = new();

    /// <summary>
    /// Единый конфиг длительностей/порогов временных запахов (smellSystemConfig).
    /// </summary>
    private SmellSystemConfigPrototype _config = default!;

    [ValidatePrototypeId<SmellSystemConfigPrototype>]
    private const string ConfigId = "Default";

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        RebuildProtoCache();
    }

    public IReadOnlyList<StatusScentPrototype> StatusScentProtos => _statusScentProtos;

    /// <summary>
    /// Текущий конфиг системы запахов.
    /// </summary>
    public SmellSystemConfigPrototype Config => _config;

    /// <summary>
    /// Пересобирает кэш прототипов статус-запахов и конфиг системы.
    /// </summary>
    private void RebuildProtoCache()
    {
        _statusScentProtos = _prototypes.EnumeratePrototypes<StatusScentPrototype>().ToList();
        _config = _prototypes.Index<SmellSystemConfigPrototype>(ConfigId);
    }

    /// <summary>
    /// Обработчик горячей перезагрузки прототипов (reloadprototypes).
    /// Пересобирает кэш статус-запахов и конфиг только если менялись именно
    /// StatusScentPrototype или SmellSystemConfigPrototype — правки статус-запахов и
    /// баланса подхватываются без перезапуска сервера.
    /// </summary>
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.ByType.ContainsKey(typeof(StatusScentPrototype))
            && !args.ByType.ContainsKey(typeof(SmellSystemConfigPrototype)))
            return;

        RebuildProtoCache();
    }
}
