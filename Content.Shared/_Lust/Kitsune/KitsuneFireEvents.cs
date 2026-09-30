using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Kitsune;

public sealed partial class KitsuneFireActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class KitsuneFireDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class KitsuneFoxLightsDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class KitsuneFoxLightsActionEvent : InstantActionEvent
{
}

