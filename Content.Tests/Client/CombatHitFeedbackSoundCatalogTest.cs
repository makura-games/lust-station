using System.Collections.Generic;
using Content.Client._Sunrise.CombatMode;
using NUnit.Framework;

namespace Content.Tests.Client;

[TestFixture]
public sealed class CombatHitFeedbackSoundCatalogTest
{
    [Test]
    public void InvalidSoundIdsFallBackToConfiguredDefaultOrFirstAvailableSound()
    {
        var available = new[] { "click", "bonk" };

        Assert.Multiple(() =>
        {
            Assert.That(CombatHitFeedbackSoundCatalog.ResolveSoundId("missing", available, "bonk"), Is.EqualTo("bonk"));
            Assert.That(CombatHitFeedbackSoundCatalog.ResolveSoundId("missing", available, "also-missing"), Is.EqualTo("click"));
            Assert.That(CombatHitFeedbackSoundCatalog.ResolveSoundId("bonk", available, "click"), Is.EqualTo("bonk"));
            Assert.That(CombatHitFeedbackSoundCatalog.NormalizeVolume(-1f), Is.EqualTo(0f));
            Assert.That(CombatHitFeedbackSoundCatalog.NormalizeVolume(2f), Is.EqualTo(1f));
            Assert.That(CombatHitFeedbackSoundCatalog.NormalizeVolume(float.NaN), Is.EqualTo(0.75f));
        });
    }
}
