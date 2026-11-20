using Content.Server._Lust.Toys.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Shared._Lust.Toys.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Jittering;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Speech.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Lust.DeviceLinking.Systems;

public sealed partial class LinkVibratingToySystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _signalSystem = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<VibratingToyComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<VibratingToyComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<VibratingToyComponent, GetVerbsEvent<ActivationVerb>>(AddToggleVibratingVerb);
    }

    private void OnInit(Entity<VibratingToyComponent> toyControl, ref MapInitEvent args)
    {
        _signalSystem.EnsureSinkPorts(toyControl, toyControl.Comp.TogglePort, toyControl.Comp.OnPort, toyControl.Comp.OffPort);
    }

    // Порты, переключающие игрушку
    private void OnSignalReceived(Entity<VibratingToyComponent> toyControl, ref SignalReceivedEvent args)
    {
        if (!TryComp<VibratingToyComponent>(toyControl, out var toy))
            return;

        if (args.Port == toyControl.Comp.TogglePort)
            SetEnabled(toyControl, toy, !toy.Enabled);

        if (args.Port == toyControl.Comp.OnPort)
            SetEnabled(toyControl, toy, true);

        if (args.Port == toyControl.Comp.OffPort)
            SetEnabled(toyControl, toy, false);

        Dirty(toyControl, toy);
    }

    private void AddToggleVibratingVerb(Entity<VibratingToyComponent> toyControl, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract)
            return;

        if (!TryComp<VibratingToyComponent>(toyControl, out var toy))
            return;

        var @event = args;
        ActivationVerb verb = new()
        {
            Text = Loc.GetString("vibrating-toy-verb-toggle"),
            Act = () => SetEnabled(toyControl, toy, !toy.Enabled)
        };
        args.Verbs.Add(verb);
        Dirty(toyControl, toy);
    }
    public void SetEnabled(EntityUid uid, VibratingToyComponent component, bool status)
    {
        component.Enabled = status;

        if (!status)
        {
            if (component.IsEquipped)
            {
                RemCompDeferred<VibratingComponent>(Transform(uid).ParentUid);
                RemCompDeferred<StutteringAccentComponent>(Transform(uid).ParentUid);
                RemCompDeferred<JitteringComponent>(Transform(uid).ParentUid);

                if (component.BaseWalkSpeed == null || component.BaseSprintSpeed == null || component.BaseAcceleration == null)
                    return;

                if (!TryComp<MovementSpeedModifierComponent>(Transform(uid).ParentUid, out var modifierComponent))
                    return;

                _movementSpeedModifier.ChangeBaseSpeed(Transform(uid).ParentUid,
                component.BaseWalkSpeed.Value,
                component.BaseSprintSpeed.Value,
                component.BaseAcceleration.Value);

                component.BaseWalkSpeed = null;
                component.BaseSprintSpeed = null;
                component.BaseAcceleration = null;
            }
            component.PlayingStream = _audio.Stop(component.PlayingStream);
            component.PlayingStream = null;
            Dirty(uid, component);
        }
        else
        {
            if (component.IsEquipped)
            {
                EnsureComp<VibratingComponent>(Transform(uid).ParentUid).Toy = uid;
                EnsureComp<StutteringAccentComponent>(Transform(uid).ParentUid);

                if (component.BaseWalkSpeed != null || component.BaseSprintSpeed != null || component.BaseAcceleration != null)
                    return;

                if (!TryComp<MovementSpeedModifierComponent>(Transform(uid).ParentUid, out var modifierComponent))
                    return;

                component.BaseSprintSpeed = modifierComponent.BaseSprintSpeed;
                component.BaseWalkSpeed = modifierComponent.BaseWalkSpeed;
                component.BaseAcceleration = modifierComponent.Acceleration;

                _movementSpeedModifier.ChangeBaseSpeed(
                    Transform(uid).ParentUid,
                    component.TargetWalkSpeed,
                    component.TargetSprintSpeed,
                    component.TargetAcceleration);
            }
            if (component.ActiveSound != null && component.PlayingStream == null)
            {
                var audio = component.ActiveSound.Params
                    .WithLoop(true)
                    .WithVolume(component.Volume)
                    .WithMaxDistance(component.MaxDistance);
                var stream = _audio.PlayPvs(component.ActiveSound, uid, audio);
                if (stream?.Entity is { } entity)
                    component.PlayingStream = entity;
                Dirty(uid, component);
            }
        }
    }
}
