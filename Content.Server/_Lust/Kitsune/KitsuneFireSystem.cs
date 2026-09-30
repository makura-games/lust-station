using Content.Shared._Lust.Kitsune;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Robust.Shared.Timing;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;

using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.Kitsune
{
    public sealed partial class KitsuneFireSystem : EntitySystem
    {
        [Dependency] private DamageableSystem _damageable = default!;
        [Dependency] private IGameTiming _timing = default!;
        [Dependency] private SharedDoAfterSystem _doAfter = default!;
        [Dependency] private SharedAudioSystem _audio = default!;
        [Dependency] private SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();
            UpdatesAfter.Add(typeof(FlammableSystem));
            SubscribeLocalEvent<KitsuneFireComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<KitsuneFireComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<KitsuneFireActionEvent>(OnAction);
            SubscribeLocalEvent<KitsuneFireDoAfterEvent>(OnDoAfter);
        }

        private void OnAction(KitsuneFireActionEvent args)
        {
            if (args.Handled)
                return;

            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Performer, TimeSpan.FromSeconds(2), new KitsuneFireDoAfterEvent(), args.Performer, args.Target, args.Target)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = false
            });

            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/butcher.ogg"), args.Performer);
            args.Handled = true;
        }

        private void OnDoAfter(KitsuneFireDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || args.Args.Target == null)
                return;

            // Damage the performer (Self-Damage)
            var damage = new DamageSpecifier()
            {
                DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
                {
                    { "Slash", FixedPoint2.New(9) }
                }
            };
            _damageable.TryChangeDamage(args.Args.User, damage);

            // Apply Effect
            var target = args.Args.Target.Value;
            EnsureComp<KitsuneFireComponent>(target);
            _appearance.SetData(target, FireVisuals.OnFire, true);
            _appearance.SetData(target, FireVisuals.FireStacks, 5f);

            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/enter_blood.ogg"), target);
            args.Handled = true;
        }

        private void OnInit(EntityUid uid, KitsuneFireComponent component, ComponentInit args)
        {
            component.NextTick = _timing.CurTime + TimeSpan.FromSeconds(1);
            component.Duration = _timing.CurTime + TimeSpan.FromSeconds(5);
        }

        private void OnShutdown(EntityUid uid, KitsuneFireComponent component, ComponentShutdown args)
        {
            _appearance.SetData(uid, FireVisuals.OnFire, false);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<KitsuneFireComponent, DamageableComponent>();
            while (query.MoveNext(out var uid, out var fire, out var damageable))
            {
                if (_timing.CurTime > fire.Duration)
                {
                    RemComp<KitsuneFireComponent>(uid);
                    continue;
                }

                // Force visual state to true to override FlammableSystem
                _appearance.SetData(uid, FireVisuals.OnFire, true);
                _appearance.SetData(uid, FireVisuals.FireStacks, 5f);

                if (_timing.CurTime < fire.NextTick)
                    continue;

                fire.NextTick = _timing.CurTime + TimeSpan.FromSeconds(1);
                _damageable.TryChangeDamage(uid, fire.Healing, ignoreResistances: true, interruptsDoAfters: false);
            }
        }
    }
}
