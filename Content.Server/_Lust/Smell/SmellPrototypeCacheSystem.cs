using System.Linq;
using Content.Shared._Lust.Smell.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.Smell;

/// <summary>
/// Общий кэш прототипов системы запахов: список статус-запахов (statusScent).
/// Вынесен в отдельную систему, чтобы и «наделение» (ScentAcquisitionSystem), и «нюханье»
/// (SmellSystem) читали одни и те же данные без дублирования. Пересобирается и при старте,
/// и при хот-релоаде прототипов.
/// </summary>
public sealed class SmellPrototypeCacheSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    /// <summary>
    /// Список статус-эффект->запах из YAML (statusScent).
    /// </summary>
    private List<StatusScentPrototype> _statusScentProtos = new();

    public override void Initialize()
    {
        // Чтобы изменения статус-запахов подхватывались на живом сервере
        // (reloadprototypes), а не только при перезапуске, пересобираем кэш и при релоаде.
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        RebuildProtoCache();
    }

    public IReadOnlyList<StatusScentPrototype> StatusScentProtos => _statusScentProtos;

    /// <summary>
    /// Пересобирает кэш прототипов статус-запахов.
    /// </summary>
    private void RebuildProtoCache()
    {
        _statusScentProtos = _prototypes.EnumeratePrototypes<StatusScentPrototype>().ToList();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.ByType.ContainsKey(typeof(StatusScentPrototype)))
            return;

        RebuildProtoCache();
    }
}
