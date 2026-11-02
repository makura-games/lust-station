using Content.Shared.DoAfter;

namespace Content.Shared.Lock;

public sealed partial class LockSystem
{
    public bool LustTryLock(EntityUid uid, EntityUid user, LockComponent? lockComp = null, bool skipDoAfter = false, bool skipChecks = false)
    {
        if (!Resolve(uid, ref lockComp))
            return false;

        if (!CanToggleLock(uid, user, quiet: false))
            return false;

        if (lockComp.MindShieldLock && !HasMindshield(uid, user, quiet: false))
            return false;
        if (!skipChecks)
        {
            if (!CanToggleLock(uid, user, quiet: false))
                return false;

            if (lockComp.UseAccess && !HasUserAccess(uid, user, false))
                return false;
            if (lockComp.MindShieldLock && !HasMindshield(uid, user, quiet: false))
                return false;

            if (lockComp.UseAccess && !HasUserAccess(uid, user, false))
                return false;
        }
        if (!skipDoAfter && lockComp.LockTime != TimeSpan.Zero)
        {
            return _doAfter.TryStartDoAfter(
                new DoAfterArgs(EntityManager, user, lockComp.LockTime, new LockDoAfter(), uid, uid)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                    BreakOnDropItem = false,
                });
        }

        Lock(uid, user, lockComp);
        return true;
    }

    public bool LustTryUnlock(EntityUid uid,
        EntityUid user,
        LockComponent? lockComp = null,
        bool skipDoAfter = false,
        bool skipChecks = false)
    {
        if (!Resolve(uid, ref lockComp))
            return false;

        if (!skipChecks)
        {
            if (!CanToggleLock(uid, user, quiet: false))
                return false;

            if (lockComp.MindShieldLock && !HasMindshield(uid, user, quiet: false))
                return false;

            if (lockComp.UseAccess && !HasUserAccess(uid, user, false))
                return false;
        }

        if (!skipDoAfter && lockComp.UnlockTime != TimeSpan.Zero)
        {
            return _doAfter.TryStartDoAfter(
                new DoAfterArgs(EntityManager, user, lockComp.LockTime, new UnlockDoAfter(), uid, uid)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                    BreakOnDropItem = false,
                });
        }

        Unlock(uid, user, lockComp);
        return true;
    }
}
