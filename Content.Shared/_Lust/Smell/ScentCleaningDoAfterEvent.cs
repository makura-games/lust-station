using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Smell;

/// <summary>
/// DoAfter «мытьё запахов»: вызывается после того, как игрок применил предмет
/// с ScentCleaningComponent (мыло) к цели. Обработчик в ScentCleaningSystem
/// смывает временные запахи и ставит временную маскировку основного запаха.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ScentCleaningDoAfterEvent : SimpleDoAfterEvent
{
}