// Lust edit
using System.Numerics;
using Content.Server._Lust.Administration;
using Content.Shared.Administration;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Database;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

/// <summary>
/// Lust — админская кара «Кабанизатор»:
/// спавнит костёр со столбом (BonfireStake), пристёгивает к нему цель без возможности открепиться,
/// превращает в статичную свинью под песню «Шашлычок» и заставляет крутиться, хрюкать и пукать.
/// </summary>
public sealed partial class AdminVerbSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;

    private static readonly SpriteSpecifier KabanizatorVerbIcon =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/Mobs/Animals/pig.rsi"), "pig");

    /// <summary>
    /// Добавляет контекстное действие кары «Кабанизатор» для администраторов с правами Fun.
    /// </summary>
    private void AddKabanizatorSmiteVerb(GetVerbsEvent<Verb> args)
    {
        if (!CanKabanizatorSmite(args.User, args.Target))
            return;

        var kabanizatorName = Loc.GetString("admin-smite-kabanizator-name").ToLowerInvariant();
        Verb kabanizator = new()
        {
            Text = kabanizatorName,
            Category = VerbCategory.Smite,
            Icon = KabanizatorVerbIcon,
            Act = () => TryKabanizatorSmite(args.User, args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", kabanizatorName, Loc.GetString("admin-smite-kabanizator-description"))
        };
        args.Verbs.Add(kabanizator);
    }

    /// <summary>
    /// Проверяет возможность применить кару «Кабанизатор» к цели.
    /// </summary>
    public bool CanKabanizatorSmite(EntityUid user, EntityUid target)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return false;

        var player = actor.PlayerSession;
        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return false;

        if (HasComp<MapComponent>(target) || HasComp<MapGridComponent>(target))
            return false;

        return true;
    }

    /// <summary>
    /// Пытается применить кару «Кабанизатор» к цели.
    /// </summary>
    public bool TryKabanizatorSmite(EntityUid user, EntityUid target)
    {
        if (!CanKabanizatorSmite(user, target))
            return false;

        var coords = Transform(target).Coordinates;
        var targetName = Name(target);

        // 1. Спавним костёр со столбом (BonfireStake)
        var bonfire = Spawn("BonfireStake", coords);

        // 2. Врубаем легендарный звук «Шашлычок» от костра
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Lust/Effects/shashlychok.ogg"), bonfire, AudioParams.Default.WithVolume(3.0f).WithMaxDistance(25f));

        // 3. Первичный сочный звук пуканья
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Lust/Effects/fart.ogg"), coords, AudioParams.Default.WithVolume(4.5f));

        // 4. Оживляем и лечим цель, чтобы даже мёртвые или критованные превращались в здоровую свинью
        RaiseLocalEvent(target, new RejuvenateEvent());

        // 5. Полиморф в свинью
        var pig = _polymorphSystem.PolymorphEntity(target, "LustAdminPigSmite");
        if (pig == null)
        {
            Del(bonfire);
            return false;
        }

        var resultEnt = pig.Value;
        RaiseLocalEvent(resultEnt, new RejuvenateEvent());

        // 6. Делаем свинью абсолютно статичным объектом, который невозможно подвинуть
        if (TryComp<PhysicsComponent>(resultEnt, out var physics))
        {
            _physics.SetLinearVelocity(resultEnt, Vector2.Zero, body: physics);
            _physics.SetAngularVelocity(resultEnt, 0f, body: physics);
            _physics.SetBodyType(resultEnt, BodyType.Static, body: physics);
            _physics.SetFixedRotation(resultEnt, true, body: physics);
        }

        // 7. Лишаем возможности двигаться и быть перемещённым
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(resultEnt);
        _movementSpeedModifierSystem.ChangeBaseSpeed(resultEnt, 0f, 0f, 0f, movementSpeed);
        RemComp<PullableComponent>(resultEnt);

        // 8. Навешиваем BuckleComponent и пристёгиваем свинью к столбу костра (user = null, чтобы призрак админа без рук не блокировал пристёгивание)
        EnsureComp<BuckleComponent>(resultEnt);
        Timer.Spawn(TimeSpan.FromMilliseconds(250), () =>
        {
            if (Deleted(resultEnt) || Deleted(bonfire))
                return;

            _buckle.TryBuckle(resultEnt, null, bonfire);
        });

        // 9. Навешиваем компонент вращения, хрюканья, пуканья и блокировки отстёгивания
        var victimComp = EnsureComp<KabanizatorVictimComponent>(resultEnt);
        victimComp.NextOinkTime = _gameTiming.CurTime + TimeSpan.FromSeconds(1.5f);
        victimComp.NextFartTime = _gameTiming.CurTime + TimeSpan.FromSeconds(2.5f);

        // 10. Попапы
        _popupSystem.PopupEntity(Loc.GetString("admin-smite-kabanizator-self"), resultEnt, resultEnt, PopupType.LargeCaution);
        _popupSystem.PopupCoordinates(
            Loc.GetString("admin-smite-kabanizator-others", ("name", targetName)),
            coords,
            Filter.PvsExcept(resultEnt),
            true,
            PopupType.MediumCaution);

        _adminLogs.Add(LogType.AdminCommands, LogImpact.Extreme, $"{ToPrettyString(user):actor} кабанизировал {ToPrettyString(target):target} в свинью на столбе костра");
        return true;
    }
}
