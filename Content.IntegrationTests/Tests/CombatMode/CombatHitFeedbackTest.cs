using Content.IntegrationTests.Fixtures;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.CombatMode;

[TestFixture]
public sealed class CombatHitFeedbackTest : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings { Connected = true, DummyTicker = false };

    private static readonly ProtoId<DamageTypePrototype> DamageType = "Blunt";
    private static readonly System.Reflection.FieldInfo AudioFileName =
        typeof(AudioComponent).GetField(nameof(AudioComponent.FileName))!;

    [Test]
    public async Task AttributedDamagePlaysOnTheAttackingClient()
    {
        var pair = Pair;
        var map = await pair.CreateTestMap();
        EntityUid target = default;
        var attacker = ServerSession!.AttachedEntity!.Value;

        await OverrideCVar(Side.Client, SunriseCCVars.CombatHitFeedbackHitEnabled, true);
        await OverrideCVar(Side.Client, SunriseCCVars.CombatHitFeedbackCriticalEnabled, true);
        await OverrideCVar(Side.Client, SunriseCCVars.CombatHitFeedbackKillEnabled, true);

        await Server.WaitPost(() => target = Server.EntMan.SpawnAtPosition("MobHuman", map.GridCoords));
        await pair.RunTicksSync(2);

        foreach (var (damage, expectedState, expectedSound) in new[]
        {
            (1, MobState.Alive, "hit"),
            (110, MobState.Critical, "critical"),
            (1, MobState.Critical, "critical"),
            (200, MobState.Dead, "kill"),
        })
        {
            await Server.WaitPost(() =>
            {
                var damageType = Server.ProtoMan.Index(DamageType);
                Server.System<DamageableSystem>().ChangeDamage(
                    target,
                    new DamageSpecifier(damageType, FixedPoint2.New(damage)),
                    origin: attacker,
                    ignoreGlobalModifiers: true,
                    ignoreVariance: true);
                Assert.That(Server.EntMan.GetComponent<MobStateComponent>(target).CurrentState,
                    Is.EqualTo(expectedState));
            });
            await pair.RunTicksSync(3);

            await Client.WaitPost(() =>
            {
                Assert.That(CountAudioComponents(CEntMan, expectedSound), Is.EqualTo(1), expectedSound);
                Assert.That(CountFeedbackAudioComponents(CEntMan), Is.EqualTo(1),
                    "Feedback sounds must not overlap");
            });
        }
    }

    private static int CountAudioComponents(IEntityManager entityManager, string sound)
    {
        var count = 0;
        var query = entityManager.EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var audio))
        {
            if (AudioFileName.GetValue(audio) is string fileName &&
                fileName == $"/Audio/Effects/HitSounds/{sound}.ogg")
                count++;
        }

        return count;
    }

    private static int CountFeedbackAudioComponents(IEntityManager entityManager)
    {
        var count = 0;
        var query = entityManager.EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var audio))
        {
            if (AudioFileName.GetValue(audio) is string fileName &&
                fileName.StartsWith("/Audio/Effects/HitSounds/", System.StringComparison.Ordinal))
                count++;
        }

        return count;
    }
}
