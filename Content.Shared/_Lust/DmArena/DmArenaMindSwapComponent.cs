using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Shared._Lust.DmArena;

/// <summary>
/// Нужен, когда разуму надо побыть в другом теле, не затрагивая основной разум и не наследуя его знания.
/// </summary>
[RegisterComponent]
public sealed partial class DmArenaMindSwapComponent : Component
{
    /// <summary>
    /// Настоящий разум игрока, отложенный на время подмены.
    /// </summary>
    [ViewVariables]
    public EntityUid OriginalMind;

    /// <summary>
    /// Одноразовый разум, который играет в теле арены.
    /// </summary>
    [ViewVariables]
    public EntityUid DisposableMind;

    /// <summary>
    /// Игрок, которому принадлежал настоящий разум в момент подмены.
    /// </summary>
    [ViewVariables]
    public NetUserId? OriginalUserId;
}
