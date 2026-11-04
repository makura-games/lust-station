using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Shared._Lust.DmArena;

[RegisterComponent]
public sealed partial class DmArenaDormantBodyComponent : Component
{
    /// <summary>
    /// Игрок, чьё прежнее тело помечено.
    /// </summary>
    [ViewVariables]
    public NetUserId UserId;
}
