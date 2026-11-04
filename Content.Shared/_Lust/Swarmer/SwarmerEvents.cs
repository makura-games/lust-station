using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Swarmer;

[Serializable, NetSerializable]
public sealed partial class SwarmerDisintegrateDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SwarmerDismantleDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SwarmerBarricadeDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SwarmerSelfRepairDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SwarmerTeleportDoAfterEvent : SimpleDoAfterEvent;

public sealed partial class SwarmerTeleportActionEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class SwarmerReplicateDoAfterEvent : SimpleDoAfterEvent;

public sealed partial class SwarmerReplicateActionEvent : InstantActionEvent;

public sealed partial class SwarmerEscapeActionEvent : InstantActionEvent;

public sealed partial class SwarmerBarricadeActionEvent : InstantActionEvent;

public sealed partial class SwarmerTrapActionEvent : WorldTargetActionEvent;

public sealed partial class SwarmerSelfRepairActionEvent : InstantActionEvent;

public sealed partial class SwarmerToggleLightActionEvent : InstantActionEvent;
