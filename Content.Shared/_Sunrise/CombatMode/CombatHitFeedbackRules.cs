using Content.Shared.Mobs;

namespace Content.Shared._Sunrise.CombatMode;

public static class CombatHitFeedbackRules
{
    public static CombatHitFeedbackType? ClassifyDamage(MobState state, bool damageIncreased, bool hasOrigin)
    {
        if (!damageIncreased || !hasOrigin || state is not (MobState.Alive or MobState.Critical))
            return null;

        return CombatHitFeedbackType.Hit;
    }

    public static CombatHitFeedbackType? ClassifyStateChange(MobState previous, MobState current, bool hasOrigin)
    {
        if (!hasOrigin || previous == current)
            return null;

        return current switch
        {
            MobState.Critical => CombatHitFeedbackType.Critical,
            MobState.Dead => CombatHitFeedbackType.Kill,
            _ => null,
        };
    }

    public static bool HasPriorityOverOrEqual(CombatHitFeedbackType incoming, CombatHitFeedbackType? current)
    {
        return current == null || incoming >= current.Value;
    }
}
