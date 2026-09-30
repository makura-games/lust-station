using Content.Shared._Lust.Kitsune;
using Robust.Shared.Timing;
using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Shared.Popups;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.Kitsune
{
    public sealed partial class KitsuneFoxLightsSystem : EntitySystem
    {
        [Dependency] private IGameTiming _timing = default!;
        [Dependency] private SharedTransformSystem _transform = default!;
        [Dependency] private SharedDoAfterSystem _doAfter = default!;
        [Dependency] private DamageableSystem _damageable = default!;
        [Dependency] private SharedAudioSystem _audio = default!;
        [Dependency] private SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<KitsuneFoxLightsComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<KitsuneFoxLightsActionEvent>(OnAction);
            SubscribeLocalEvent<KitsuneFoxLightsDoAfterEvent>(OnDoAfter);
        }

        private void OnAction(KitsuneFoxLightsActionEvent args)
        {
            if (args.Handled) return;

            var comp = EnsureComp<KitsuneFoxLightsComponent>(args.Performer);
            //comp.Orbs.RemoveAll(orb => Deleted(orb) || Terminating(orb));

            if (comp.Orbs.Count >= 3)
            {
                _popup.PopupEntity(Loc.GetString("kitsune-lights-max"), args.Performer, args.Performer);
                return;
            }

            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/butcher.ogg"), args.Performer);

            var doAfterArgs = new DoAfterArgs(EntityManager, args.Performer, comp.CastTime, new KitsuneFoxLightsDoAfterEvent(), args.Performer)
            {
                BreakOnMove = false,
                BreakOnDamage = false,
                NeedHand = false,
                Broadcast = true // FIsh edit: necessary for broadcast system subscription to run since component is deleted in update
            };

            _doAfter.TryStartDoAfter(doAfterArgs);

            args.Handled = true;
        }

        private void OnDoAfter(KitsuneFoxLightsDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled)
                return;

            var user = args.User;
            var component = EnsureComp<KitsuneFoxLightsComponent>(user);

            //component.Orbs.RemoveAll(orb => Deleted(orb) || Terminating(orb));

            if (component.Orbs.Count < 3)
            {
                var spawnedOrbs = SpawnOrbs(user, component);
                foreach (var orb in spawnedOrbs)
                {
                    if (TryComp<KitsuneFoxLightsOrbComponent>(orb, out var orbComp))
                    {
                        orbComp.DieAt = _timing.CurTime + component.LightDuration;
                    }
                }
            }

            // Play Success Sound
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/enter_blood.ogg"), user);

            // Damage the performer
            var damage = new DamageSpecifier()
            {
                DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
                {
                    { "Slash", FixedPoint2.New(3) }
                }
            };
            _damageable.TryChangeDamage(user, damage, ignoreResistances: true);
            args.Handled = true;
        }

        private List<EntityUid> SpawnOrbs(EntityUid uid, KitsuneFoxLightsComponent component)
        {
            var xform = Transform(uid);
            var coords = xform.Coordinates;
            var spawnedOrbs = new List<EntityUid>();

            for (var i = 0; i < 1; i++)
            {
                var orb = Spawn("KitsuneFoxLight", coords);
                var orbComp = EnsureComp<KitsuneFoxLightsOrbComponent>(orb);
                orbComp.Parent = uid;
                orbComp.Angle = 0;
                orbComp.Radius = 1.5f;
                orbComp.Speed = 1f;

                component.Orbs.Add(orb);
                spawnedOrbs.Add(orb);
            }

            return spawnedOrbs;
        }

        private void DeleteOrbAndCleanupParent(EntityUid orbUid, KitsuneFoxLightsOrbComponent orbComp)
        {
            if (TryComp<KitsuneFoxLightsComponent>(orbComp.Parent, out var parentComp))
            {
                parentComp.Orbs.Remove(orbUid);
                if (parentComp.Orbs.Count == 0)
                {
                    RemCompDeferred<KitsuneFoxLightsComponent>(orbComp.Parent);
                }
            }

            QueueDel(orbUid);
        }

        private void OnShutdown(EntityUid uid, KitsuneFoxLightsComponent component, ComponentShutdown args)
        {
            foreach (var orb in component.Orbs)
            {
                if (!Terminating(orb))
                    QueueDel(orb);
            }
            component.Orbs.Clear();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Handle Duration of Orbs individually
            var query = EntityQueryEnumerator<KitsuneFoxLightsOrbComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var orb, out var xform))
            {
                if (!Exists(orb.Parent) || Terminating(orb.Parent))
                {
                    DeleteOrbAndCleanupParent(uid, orb);
                    continue;
                }

                if (_timing.CurTime > orb.DieAt)
                {
                    DeleteOrbAndCleanupParent(uid, orb);
                    continue;
                }

                // Update Angle
                orb.Angle += orb.Speed * frameTime;
                if (orb.Angle > MathF.PI * 2) orb.Angle -= MathF.PI * 2;

                // Calculate position relative to parent
                var parentXform = Transform(orb.Parent);
                if (parentXform.MapID != xform.MapID)
                {
                    DeleteOrbAndCleanupParent(uid, orb); // Parent changed map
                    continue;
                }

                var offset = new Vector2(MathF.Cos(orb.Angle), MathF.Sin(orb.Angle)) * orb.Radius;

                // We set WorldPosition to Parent + Offset.
                // Creating a smooth orbit.
                _transform.SetWorldPosition(xform, _transform.GetWorldPosition(parentXform) + offset);
            }

            // Clean up KitsuneFoxLightsComponent when all orbs are gone
            var queryComp = EntityQueryEnumerator<KitsuneFoxLightsComponent>();
            while (queryComp.MoveNext(out var uid, out var comp))
            {
                comp.Orbs.RemoveAll(orb => Deleted(orb) || Terminating(orb));
                if (comp.Orbs.Count == 0)
                {
                    RemCompDeferred<KitsuneFoxLightsComponent>(uid);
                }
            }
        }
    }
}
