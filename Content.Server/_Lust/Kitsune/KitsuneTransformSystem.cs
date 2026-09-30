using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared._Lust.Kitsune;
using Content.Shared._Lust.SpriteColor;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Inventory;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared._Sunrise.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.Kitsune;

public sealed partial class KitsuneTransformSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SpriteColorSystem _spriteColor = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SunriseHumanoidBodySystem _sunriseBody = default!;

    // Dictionary to track when each transformed entity should auto-revert
    private Dictionary<EntityUid, float> _transformDurations = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KitsuneTransformComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<KitsuneTransformComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<KitsuneTransformComponent, KitsuneTransformActionEvent>(OnKitsuneTransform);
        SubscribeLocalEvent<KitsuneTransformComponent, KitsuneTransformDoAfterEvent>(OnKitsuneTransformDoAfter);
        SubscribeLocalEvent<KitsuneTransformComponent, KitsuneRevertActionEvent>(OnKitsuneRevert);
        SubscribeLocalEvent<KitsuneTransformComponent, KitsuneRevertDoAfterEvent>(OnKitsuneRevertDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Check for expired transforms
        var expired = new List<EntityUid>();
        var toUpdate = new Dictionary<EntityUid, float>();

        foreach (var (uid, timeLeft) in _transformDurations)
        {
            var newTimeLeft = timeLeft - frameTime;
            if (newTimeLeft <= 0)
                expired.Add(uid);
            else
                toUpdate[uid] = newTimeLeft;
        }

        // Update the timers
        _transformDurations = toUpdate;

        // Auto-revert expired transforms
        foreach (var uid in expired)
        {
            if (!TryComp<KitsuneTransformComponent>(uid, out var component) ||
                !TryComp<PolymorphedEntityComponent>(uid, out var morphComp))
                continue;
            _polymorph.Revert((uid, morphComp));
            _popup.PopupEntity(Loc.GetString("kitsune-transform-expired"), uid, uid, PopupType.MediumCaution);
        }
    }

    private void OnMapInit(Entity<KitsuneTransformComponent> ent, ref MapInitEvent args)
    {
        // Grant actions defined in the component
        // This is moved here from ActionGrant to prevent loss during anomaly infection
        foreach (var actionProto in ent.Comp.Actions)
        {
            EntityUid? actionEnt = null;
            _actions.AddAction(ent.Owner, ref actionEnt, actionProto);

            if (actionEnt != null)
            {
                ent.Comp.ActionEntities.Add(actionEnt.Value);

                // If this is the revert action, set its icon to the parent entity (the humanoid)
                // This makes it look like the vanilla revert action
                if (TryComp<PolymorphedEntityComponent>(ent.Owner, out var morphComp) &&
                    _actions.GetAction(actionEnt.Value) is { } action &&
                    _actions.GetEvent(actionEnt.Value) is KitsuneRevertActionEvent)
                {
                    _actions.SetEntityIcon((actionEnt.Value, action.Comp), morphComp.Parent);
                }
            }
        }
    }

    private void OnShutdown(Entity<KitsuneTransformComponent> ent, ref ComponentShutdown args)
    {
        // Remove actions granted by this component
        // guard against QueueDel on already-terminating entities (client-side prediction error)
        foreach (var actionEnt in ent.Comp.ActionEntities)
        {
            if (TerminatingOrDeleted(actionEnt))
                continue;
            _actions.RemoveAction(ent.Owner, actionEnt);
        }

        _transformDurations.Remove(ent.Owner);
    }

    private void OnKitsuneTransform(Entity<KitsuneTransformComponent> ent, ref KitsuneTransformActionEvent args)
    {
        args.Handled = true;

        if (TryComp<PolymorphedEntityComponent>(ent.Owner, out _))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-already-transformed"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        // Start the do-after
        var doAfterArgs = new DoAfterArgs(EntityManager, ent.Owner, TimeSpan.FromSeconds(ent.Comp.Delay),
            new KitsuneTransformDoAfterEvent(),
            ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1f,
            NeedHand = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;
        _popup.PopupEntity(Loc.GetString("kitsune-transform-starting"), ent.Owner, ent.Owner, PopupType.MediumCaution);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/butcher.ogg"), ent.Owner);
    }

    private void OnKitsuneTransformDoAfter(Entity<KitsuneTransformComponent> ent, ref KitsuneTransformDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        // Transform into fox
        if (!_prototypeManager.TryIndex<PolymorphPrototype>(new ProtoId<PolymorphPrototype>("KitsuneTransform"), out var prototype))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-failed"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        // Apply 9 slash damage to self
        var damage = new DamageSpecifier()
        {
            DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
            {
                { "Slash", FixedPoint2.New(9) },
            },
        };
        _damage.TryChangeDamage(ent.Owner, damage);
        // Store the original entity reference before polymorph
        ent.Comp.StashedHumanoid = ent.Owner;

        // Extract radio channels from ears slot before polymorph
        var channels = new HashSet<ProtoId<RadioChannelPrototype>>();
        if (TryComp<InventoryComponent>(ent.Owner, out var invComp) &&
            _inventory.TryGetSlotEntity(ent.Owner, "ears", out var headsetUid, invComp))
        {
            if (TryComp<EncryptionKeyHolderComponent>(headsetUid, out var keyHolder))
            {
                channels.UnionWith(keyHolder.Channels);
            }
        }

        // Perform polymorph
        var newUid = _polymorph.PolymorphEntity(ent.Owner, prototype) ?? throw new ArgumentNullException("_polymorph.PolymorphEntity(uid, prototype)");

        // Set transform duration timer
        _transformDurations[newUid] = ent.Comp.Duration;

        // Apply intrinsic radio if we found any channels
        if (channels.Count > 0)
        {
            var activeRadio = EnsureComp<ActiveRadioComponent>(newUid);
            activeRadio.Channels.UnionWith(channels);

            var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(newUid);
            transmitter.Channels.UnionWith(channels);

            EnsureComp<IntrinsicRadioReceiverComponent>(newUid);
        }

        // Transfer TTS voice to the fox form from the original humanoid's voice
        if (TryComp<TTSComponent>(ent.Owner, out var originalTts))
        {
            if (TryComp<TTSComponent>(newUid, out var foxTts))
                foxTts.VoicePrototypeId = originalTts.VoicePrototypeId;
        }

        // Apply the humanoid's eye/hair color to the colored fur layers (idle and moving)
        if (TryComp<HumanoidProfileComponent>(ent.Owner, out var humanoid))
        {
            var eyeColor = _sunriseBody.GetEyeColor(ent.Owner);
            _spriteColor.SetStateColor(newUid, "nine-tail_fox_gray_color", eyeColor);
            _spriteColor.SetStateColor(newUid, "fox-moving-color", eyeColor);
        }

        // Transfer JobStatus icon from humanoid to fox form so role icons display for HUDs
        if (TryComp<JobStatusComponent>(ent.Owner, out var originalJobStatus) &&
            TryComp<JobStatusComponent>(newUid, out var foxJobStatus))
        {
            foxJobStatus.JobStatusIcon = originalJobStatus.JobStatusIcon;
            foxJobStatus.IsCrew = originalJobStatus.IsCrew;
            Dirty(newUid, foxJobStatus);
        }

        _popup.PopupEntity(Loc.GetString("kitsune-transform-success"), newUid, newUid, PopupType.MediumCaution);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/enter_blood.ogg"), newUid);
    }

    private void OnKitsuneRevert(Entity<KitsuneTransformComponent> ent, ref KitsuneRevertActionEvent args)
    {
        args.Handled = true;

        if (!TryComp<PolymorphedEntityComponent>(ent.Owner, out _))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-revert-not-transformed"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        // Start the do-after for revert
        var doAfterArgs = new DoAfterArgs(EntityManager, ent.Owner, TimeSpan.FromSeconds(3),
            new KitsuneRevertDoAfterEvent(),
            ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1f,
            NeedHand = false,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-revert-starting"), ent.Owner, ent.Owner, PopupType.MediumCaution);
        }
    }

    private void OnKitsuneRevertDoAfter(Entity<KitsuneTransformComponent> ent, ref KitsuneRevertDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        // Clear the duration timer
        _transformDurations.Remove(ent.Owner);

        // Revert the polymorph
        if (!TryComp<PolymorphedEntityComponent>(ent.Owner, out var morphComp))
            return;
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/enter_blood.ogg"), ent.Owner);
        _popup.PopupEntity(Loc.GetString("kitsune-revert-success"), ent.Owner, ent.Owner, PopupType.MediumCaution);
        _polymorph.Revert((ent.Owner, morphComp));
    }
}
