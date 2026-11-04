using Robust.Shared.GameObjects;

namespace Content.Shared._Lust.DmArena;

[RegisterComponent]
public sealed partial class DmArenaFighterComponent : Component
{
    /// <summary>
    /// Сущность правила арены, к которому привязан игрок.
    /// </summary>
    [ViewVariables]
    public EntityUid? RuleEntity;

    /// <summary>
    /// Последний участник арены, атаковавший игрока.
    /// </summary>
    [ViewVariables]
    public EntityUid? LastAttacker;

    [ViewVariables]
    public TimeSpan LastCombat;

    /// <summary>
    /// Время последнего попадания брошенным предметом, нужно чтобы отменить нокдаун от броска.
    /// </summary>
    [ViewVariables, NonSerialized]
    public TimeSpan LastThrownHit;
}
