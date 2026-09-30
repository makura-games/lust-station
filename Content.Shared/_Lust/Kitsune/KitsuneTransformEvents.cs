using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Kitsune;

public sealed partial class KitsuneTransformActionEvent : InstantActionEvent
{
}

public sealed partial class KitsuneRevertActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class KitsuneTransformDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class KitsuneRevertDoAfterEvent : SimpleDoAfterEvent
{
}
