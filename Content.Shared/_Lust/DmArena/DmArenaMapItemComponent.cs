using Robust.Shared.GameObjects;

namespace Content.Shared._Lust.DmArena;

/// <summary>
/// Предмет или лужа, существовавшие на карте с самого начала. Очистка арены их не удаляет.
/// </summary>
[RegisterComponent]
public sealed partial class DmArenaMapItemComponent : Component;
