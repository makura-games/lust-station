using System.Numerics;
using Content.Server._Starlight.Medical.Limbs;
using Content.Server.Body.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Stunnable;
using Content.Shared._Lust.Weapons.Melee;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Lust.Weapons.Melee;

/// <summary>
/// Серверная система оружия, способного отрубать конечности и головы при атаках в ближнем бою.
/// </summary>
public sealed partial class DismemberingWeaponSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private LimbSystem _limbSystem = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StunSystem _stun = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private SharedSuicideSystem _suicide = default!;

    [Dependency] private EntityQuery<BodyComponent> _bodyQuery = default!;
    [Dependency] private EntityQuery<OrganComponent> _organQuery = default!;

    private static readonly string[] LimbCategories =
    [
        "ArmLeft",
        "ArmRight",
        "HandLeft",
        "HandRight",
        "LegLeft",
        "LegRight",
        "FootLeft",
        "FootRight"
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DismemberingWeaponComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<DismemberingWeaponComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.Handled || args.HitEntities.Count == 0)
            return;

        foreach (var hit in args.HitEntities)
        {
            TryDismember(hit, args.User, ent);
        }
    }

    public bool TryDismember(EntityUid target, EntityUid attacker, Entity<DismemberingWeaponComponent> weapon)
    {
        if (!_bodyQuery.TryComp(target, out var body) || body.Organs == null)
            return false;

        EntityUid chosenLimb = EntityUid.Invalid;
        string chosenCategory = string.Empty;

        // 1. Проверяем шанс отсечения головы
        if (_random.Prob(weapon.Comp.HeadDismemberChance))
        {
            foreach (var organUid in body.Organs.ContainedEntities)
            {
                if (_organQuery.TryComp(organUid, out var organComp) && organComp.Category?.Id == "Head")
                {
                    chosenLimb = organUid;
                    chosenCategory = "Head";
                    break;
                }
            }
        }

        // 2. Если голова не отсекается, проверяем шанс отсечения конечностей
        if (chosenLimb == EntityUid.Invalid && _random.Prob(weapon.Comp.LimbDismemberChance))
        {
            var candidates = new List<(EntityUid Uid, string Category)>();

            foreach (var organUid in body.Organs.ContainedEntities)
            {
                if (!_organQuery.TryComp(organUid, out var organComp) || organComp.Category == null)
                    continue;

                var cat = organComp.Category.Value.Id;
                foreach (var validCat in LimbCategories)
                {
                    if (cat == validCat)
                    {
                        candidates.Add((organUid, cat));
                        break;
                    }
                }
            }

            if (candidates.Count > 0)
            {
                var pick = _random.Pick(candidates);
                chosenLimb = pick.Uid;
                chosenCategory = pick.Category;
            }
        }

        if (chosenLimb == EntityUid.Invalid)
            return false;

        // Запоминаем возможные дочерние органы, которые выпадут вместе с конечностью
        var childrenToEject = new List<EntityUid>();
        if (_organQuery.TryComp(chosenLimb, out var mainOrgan))
        {
            var childCat = mainOrgan.Category?.Id switch
            {
                "ArmLeft" => "HandLeft",
                "ArmRight" => "HandRight",
                "LegLeft" => "FootLeft",
                "LegRight" => "FootRight",
                _ => null
            };

            if (childCat != null)
            {
                foreach (var organUid in body.Organs.ContainedEntities)
                {
                    if (_organQuery.TryComp(organUid, out var o) && o.Category?.Id == childCat)
                    {
                        childrenToEject.Add(organUid);
                        break;
                    }
                }
            }
        }

        var targetCoords = Transform(target).Coordinates;

        // Вызываем ампутацию
        if (!_limbSystem.Amputate((target, body), chosenLimb))
            return false;

        // 3. Отлетание конечностей со случайным разбросом направлений и силы (чтобы не складывались в человечка)
        var attackerPos = _transform.GetWorldPosition(attacker);
        var targetPos = _transform.GetWorldPosition(target);
        var baseDir = targetPos - attackerPos;
        if (baseDir.LengthSquared() < 0.001f)
            baseDir = _random.NextVector2(0.5f, 1.0f);

        var baseAngle = baseDir.ToWorldAngle();

        // Случайный угол отклонения (-45°..+45°) и вариация силы для основной конечности
        var limbAngle = baseAngle + Angle.FromDegrees(_random.NextFloat(-45f, 45f));
        var limbDir = limbAngle.ToVec();
        var limbForce = weapon.Comp.EjectForce * _random.NextFloat(0.8f, 1.35f);

        _throwing.TryThrow(chosenLimb, limbDir, baseThrowSpeed: limbForce, user: attacker);

        // Дочерние части (кисти, стопы) получают отдельный широкий разброс (-80°..+80°) и независимую силу
        foreach (var child in childrenToEject)
        {
            var childAngle = baseAngle + Angle.FromDegrees(_random.NextFloat(-80f, 80f));
            var childDir = childAngle.ToVec();
            var childForce = weapon.Comp.EjectForce * _random.NextFloat(0.7f, 1.45f);
            _throwing.TryThrow(child, childDir, baseThrowSpeed: childForce, user: attacker);
        }

        // 4. Сочные брызги крови
        _audio.PlayPvs(weapon.Comp.DismemberSound, target);

        ReagentId bloodReagent = new("Blood", null);
        if (TryComp<BloodstreamComponent>(target, out var bloodstream) &&
            bloodstream.BloodReferenceSolution.Contents.Count > 0)
        {
            bloodReagent = bloodstream.BloodReferenceSolution.Contents[0].Reagent;
        }

        var blood = new Solution();
        blood.AddReagent(bloodReagent, FixedPoint2.New(weapon.Comp.BloodAmount));
        _puddle.TrySpillAt(targetCoords, blood, out _);

        for (var i = 0; i < weapon.Comp.SplatterCount; i++)
        {
            var offset = _random.NextVector2(0.5f, 1.3f);
            var splashCoords = targetCoords.Offset(offset);
            var splash = new Solution();
            splash.AddReagent(bloodReagent, FixedPoint2.New(8));
            _puddle.TrySpillAt(splashCoords, splash, out _);
        }

        _bloodstream.TryModifyBleedAmount(target, 16f);

        // 5. Реакция моба в зависимости от оторванной части
        var targetName = Identity.Entity(target, EntityManager);

        if (chosenCategory == "Head")
        {
            // Сбрасываем головные уборы
            if (TryComp<InventoryComponent>(target, out var inv))
            {
                _inventory.TryUnequip(target, "head", force: true, inventory: inv);
                _inventory.TryUnequip(target, "mask", force: true, inventory: inv);
                _inventory.TryUnequip(target, "eyes", force: true, inventory: inv);
                _inventory.TryUnequip(target, "ears", force: true, inventory: inv);
            }

            // Мгновенная гибель при декапитации: синхронизируем летальный урон и порог смерти
            if (TryComp<DamageableComponent>(target, out var damageable))
                _suicide.ApplyLethalDamage((target, damageable), new ProtoId<DamageTypePrototype>("Slash"));

            if (TryComp<MobThresholdsComponent>(target, out var thresholds))
                _mobThreshold.SetAllowRevives(target, false, thresholds);

            if (TryComp<MobStateComponent>(target, out var mobState) && mobState.CurrentState != MobState.Dead)
                _mobState.ChangeMobState(target, MobState.Dead, mobState);

            _popup.PopupEntity(Loc.GetString("dismember-head", ("target", targetName)), target);
        }
        else if (chosenCategory.StartsWith("Arm") || chosenCategory.StartsWith("Hand"))
        {
            _hands.DropAll(target);
            _popup.PopupEntity(Loc.GetString("dismember-arm", ("target", targetName)), target);
        }
        else if (chosenCategory.StartsWith("Leg") || chosenCategory.StartsWith("Foot"))
        {
            _stun.TryKnockdown(target, TimeSpan.FromSeconds(3.5), true);
            _popup.PopupEntity(Loc.GetString("dismember-leg", ("target", targetName)), target);
        }

        return true;
    }
}
