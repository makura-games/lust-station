using Content.Client.Actions;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Lust.Borgs;
using Content.Shared._Lust.Borgs.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Rotation;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Lust;

[TestFixture]
public sealed class BorgRestTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [Test]
    public async Task RestPersistsWithoutInput()
    {
        var borg = await SpawnControlledBorg(insertBrain: false);
        var rest = Server.System<SharedBorgRestActionSystem>();
        var rested = false;

        await Server.WaitPost(() => rested = rest.TryToggleRest((borg, null)));
        await Server.WaitAssertion(() => Assert.That(rested, Is.True));
        await Pair.RunTicksSync(180);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<BorgRestingComponent>(borg), Is.True);
            Assert.That(SEntMan.HasComponent<KnockedDownComponent>(borg), Is.False);
            var speed = SEntMan.GetComponent<MovementSpeedModifierComponent>(borg);
            Assert.That(speed.CurrentWalkSpeed, Is.Zero);
            Assert.That(speed.CurrentSprintSpeed, Is.Zero);
        });
    }

    [Test]
    public async Task RestPersistsWhenKnockdownIsCleared()
    {
        var borg = await SpawnControlledBorg(insertBrain: false);
        var rest = Server.System<SharedBorgRestActionSystem>();
        var borgNet = default(NetEntity);

        await Server.WaitPost(() =>
        {
            borgNet = SEntMan.GetNetEntity(borg);
            rest.TryToggleRest((borg, null));
        });
        await Pair.RunTicksSync(10);
        await Client.WaitAssertion(() =>
        {
            var clientBorg = CEntMan.GetEntity(borgNet);
            var sprite = CEntMan.GetComponent<SpriteComponent>(clientBorg);
            Assert.That(sprite.Rotation.Degrees, Is.EqualTo(90).Within(0.01));
        });

        await Server.WaitPost(() =>
        {
            SEntMan.AddComponent<KnockedDownComponent>(borg);
            SEntMan.RemoveComponent<KnockedDownComponent>(borg);
        });
        await Pair.RunTicksSync(10);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<BorgRestingComponent>(borg), Is.True);
            var appearance = Server.System<SharedAppearanceSystem>();
            Assert.That(appearance.TryGetData(borg, RotationVisuals.RotationState, out RotationState rotationState), Is.True);
            Assert.That(rotationState, Is.EqualTo(RotationState.Horizontal));
        });
        await Client.WaitAssertion(() =>
        {
            var clientBorg = CEntMan.GetEntity(borgNet);
            var sprite = CEntMan.GetComponent<SpriteComponent>(clientBorg);
            Assert.That(sprite.Rotation.Degrees, Is.EqualTo(90).Within(0.01));
        });
    }

    [Test]
    public async Task ActionButtonTogglesRestWithoutBrain()
    {
        var borg = await SpawnControlledBorg(insertBrain: false);
        NetEntity actionNet = default;
        NetEntity borgNet = default;

        await Server.WaitPost(() =>
        {
            var action = SEntMan.GetComponent<BorgRestActionComponent>(borg).ToggleActionEntity;
            Assert.That(action, Is.Not.Null);
            actionNet = SEntMan.GetNetEntity(action.Value);
            borgNet = SEntMan.GetNetEntity(borg);
        });

        await Pair.RunTicksSync(5);
        await Client.WaitPost(() =>
        {
            var action = CEntMan.GetEntity(actionNet);
            Client.System<ActionsSystem>().TriggerAction((action, CEntMan.GetComponent<ActionComponent>(action)));
        });

        await Pair.RunTicksSync(5);
        await Server.WaitAssertion(() => Assert.That(SEntMan.HasComponent<BorgRestingComponent>(borg), Is.True));
        await Client.WaitAssertion(() =>
        {
            var clientBorg = CEntMan.GetEntity(borgNet);
            var sprite = Client.System<SpriteSystem>();
            Assert.That(sprite.TryGetLayer((clientBorg, null), LustBorgVisualLayers.Resting, out var resting, false), Is.True);
            Assert.That(resting!.Visible, Is.True);
        });

        await Pair.RunTicksSync(180);
        await Server.WaitAssertion(() => Assert.That(SEntMan.HasComponent<BorgRestingComponent>(borg), Is.True));
        await Client.WaitAssertion(() =>
        {
            var clientBorg = CEntMan.GetEntity(borgNet);
            var sprite = Client.System<SpriteSystem>();
            Assert.That(sprite.TryGetLayer((clientBorg, null), LustBorgVisualLayers.Resting, out var resting, false), Is.True);
            Assert.That(resting!.Visible, Is.True);
        });
        await Client.WaitPost(() =>
        {
            var action = CEntMan.GetEntity(actionNet);
            Client.System<ActionsSystem>().TriggerAction((action, CEntMan.GetComponent<ActionComponent>(action)));
        });

        await Pair.RunTicksSync(60);
        await Server.WaitAssertion(() => Assert.That(SEntMan.HasComponent<BorgRestingComponent>(borg), Is.False));
    }

    [Test]
    public async Task StandingFromRestDoesNotClearExternalKnockdown()
    {
        var borg = await SpawnControlledBorg();
        var rest = Server.System<SharedBorgRestActionSystem>();
        var rested = false;
        var stood = false;

        await Server.WaitPost(() =>
        {
            rested = rest.TryToggleRest((borg, null));
            SEntMan.AddComponent<KnockedDownComponent>(borg);
            stood = rest.TryToggleRest((borg, null));
        });

        await Server.WaitAssertion(() =>
        {
            Assert.That(rested, Is.True);
            Assert.That(stood, Is.False);
            Assert.That(SEntMan.HasComponent<BorgRestingComponent>(borg), Is.True);
            Assert.That(SEntMan.HasComponent<KnockedDownComponent>(borg), Is.True);
        });
    }

    [Test]
    public async Task StandingFromRestCompletesRecovery()
    {
        var borg = await SpawnControlledBorg();
        var rest = Server.System<SharedBorgRestActionSystem>();
        var actions = Server.System<SharedActionsSystem>();
        var rested = false;
        var startedStanding = false;
        EntityUid? actionId = null;
        NetEntity borgNet = default;

        await Server.WaitPost(() =>
        {
            actionId = SEntMan.GetComponent<BorgRestActionComponent>(borg).ToggleActionEntity;
            borgNet = SEntMan.GetNetEntity(borg);
        });
        await Server.WaitAssertion(() => Assert.That(actionId, Is.Not.Null));

        await Server.WaitPost(() =>
        {
            var action = SEntMan.GetComponent<ActionComponent>(actionId!.Value);
            actions.PerformAction((borg, null), (actionId.Value, action));
            rested = SEntMan.HasComponent<BorgRestingComponent>(borg);
        });

        await Server.WaitAssertion(() => Assert.That(rested, Is.True));
        await Pair.RunTicksSync(10);
        await Client.WaitAssertion(() =>
        {
            var clientBorg = CEntMan.GetEntity(borgNet);
            var sprite = CEntMan.GetComponent<SpriteComponent>(clientBorg);
            Assert.That(sprite.Rotation.Degrees, Is.EqualTo(90).Within(0.01));
        });

        await Server.WaitPost(() => startedStanding = rest.TryToggleRest((borg, null)));
        await Server.WaitAssertion(() => Assert.That(startedStanding, Is.True));

        await Pair.RunTicksSync(45);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<BorgRestingComponent>(borg), Is.False);
            Assert.That(SEntMan.HasComponent<KnockedDownComponent>(borg), Is.False);
            var appearance = Server.System<SharedAppearanceSystem>();
            Assert.That(appearance.TryGetData(borg, RotationVisuals.RotationState, out RotationState rotationState), Is.True);
            Assert.That(rotationState, Is.EqualTo(RotationState.Vertical));
            var speed = SEntMan.GetComponent<MovementSpeedModifierComponent>(borg);
            Assert.That(speed.CurrentWalkSpeed, Is.GreaterThan(0f));
            Assert.That(speed.CurrentSprintSpeed, Is.GreaterThan(0f));
        });

        await Client.WaitAssertion(() =>
        {
            var clientBorg = CEntMan.GetEntity(borgNet);
            var sprite = CEntMan.GetComponent<SpriteComponent>(clientBorg);
            Assert.That(sprite.Rotation.Degrees, Is.EqualTo(0).Within(0.01));
        });
    }

    private async Task<EntityUid> SpawnControlledBorg(bool insertBrain = true)
    {
        var map = await Pair.CreateTestMap();
        var containers = Server.System<SharedContainerSystem>();
        EntityUid borg = default;
        var brainInserted = false;

        await Server.WaitPost(() =>
        {
            borg = SEntMan.SpawnEntity("BorgChassisGeneric", map.GridCoords);
            if (insertBrain)
            {
                var brain = SEntMan.SpawnEntity("PositronicBrain", map.GridCoords);
                var chassis = SEntMan.GetComponent<BorgChassisComponent>(borg);
                brainInserted = containers.Insert(brain, chassis.BrainContainer);
            }
            Server.PlayerMan.SetAttachedEntity(ServerSession!, borg);
        });

        if (insertBrain)
            await Server.WaitAssertion(() => Assert.That(brainInserted, Is.True));

        return borg;
    }
}
