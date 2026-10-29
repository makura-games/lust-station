using Content.Shared._Sunrise.CombatMode;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Mobs;
using NUnit.Framework;
using Robust.Shared.Configuration;

namespace Content.Tests.Shared;

[TestFixture]
public sealed class CombatHitFeedbackRulesTest
{
    [TestCase(MobState.Alive)]
    [TestCase(MobState.Critical)]
    public void LivingTargetWithPositiveDamageProducesHitFeedback(MobState state)
    {
        Assert.That(
            CombatHitFeedbackRules.ClassifyDamage(state, damageIncreased: true, hasOrigin: true),
            Is.EqualTo(CombatHitFeedbackType.Hit));
    }

    [TestCase(MobState.Alive, MobState.Critical, CombatHitFeedbackType.Critical)]
    [TestCase(MobState.Critical, MobState.Dead, CombatHitFeedbackType.Kill)]
    [TestCase(MobState.Alive, MobState.Dead, CombatHitFeedbackType.Kill)]
    public void EnteringCriticalOrDeadProducesSpecialFeedback(
        MobState previous,
        MobState current,
        CombatHitFeedbackType expected)
    {
        Assert.That(
            CombatHitFeedbackRules.ClassifyStateChange(previous, current, hasOrigin: true),
            Is.EqualTo(expected));
    }

    [Test]
    public void FeedbackCanStartWhenThereIsNoCurrentSound()
    {
        Assert.That(CombatHitFeedbackRules.HasPriorityOverOrEqual(CombatHitFeedbackType.Hit, null), Is.True);
    }

    [TestCase(CombatHitFeedbackType.Hit, CombatHitFeedbackType.Critical, true)]
    [TestCase(CombatHitFeedbackType.Critical, CombatHitFeedbackType.Kill, true)]
    [TestCase(CombatHitFeedbackType.Critical, CombatHitFeedbackType.Hit, false)]
    [TestCase(CombatHitFeedbackType.Kill, CombatHitFeedbackType.Critical, false)]
    [TestCase(CombatHitFeedbackType.Hit, CombatHitFeedbackType.Hit, true)]
    public void FeedbackOnlyReplacesAnActiveSoundWithEqualOrHigherPriority(
        CombatHitFeedbackType? current,
        CombatHitFeedbackType incoming,
        bool expected)
    {
        Assert.That(CombatHitFeedbackRules.HasPriorityOverOrEqual(incoming, current), Is.EqualTo(expected));
    }

    [Test]
    public void HealingAndUnattributedDamageProduceNoFeedback()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CombatHitFeedbackRules.ClassifyDamage(MobState.Alive, damageIncreased: false, hasOrigin: true), Is.Null);
            Assert.That(CombatHitFeedbackRules.ClassifyDamage(MobState.Alive, damageIncreased: true, hasOrigin: false), Is.Null);
            Assert.That(CombatHitFeedbackRules.ClassifyDamage(MobState.Dead, damageIncreased: true, hasOrigin: true), Is.Null);
            Assert.That(CombatHitFeedbackRules.ClassifyStateChange(MobState.Alive, MobState.Critical, hasOrigin: false), Is.Null);
            Assert.That(CombatHitFeedbackRules.ClassifyStateChange(MobState.Alive, MobState.Alive, hasOrigin: true), Is.Null);
        });
    }

    [Test]
    public void FeedbackPreferencesAreClientOnlyAndArchived()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SunriseCCVars.CombatHitFeedbackHitEnabled.Flags, Is.EqualTo(CVar.CLIENTONLY | CVar.ARCHIVE));
            Assert.That(SunriseCCVars.CombatHitFeedbackCriticalEnabled.Flags, Is.EqualTo(CVar.CLIENTONLY | CVar.ARCHIVE));
            Assert.That(SunriseCCVars.CombatHitFeedbackKillEnabled.Flags, Is.EqualTo(CVar.CLIENTONLY | CVar.ARCHIVE));
            Assert.That(SunriseCCVars.CombatHitFeedbackVolume.DefaultValue, Is.InRange(0f, 1f));
        });
    }
}
