// Lust edit
using Content.Shared.Buckle.Components;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Lust.Administration;

/// <summary>
/// Lust — система управления поведением жертвы «Кабанизатора»:
/// постоянное вращение на костре, периодическое хрюканье и пуканье, запрет расстёгивания и перемещения.
/// </summary>
public sealed partial class KabanizatorVictimSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly SoundPathSpecifier OinkSound = new("/Audio/Animals/pig_oink.ogg");
    private static readonly SoundPathSpecifier FartSound = new("/Audio/_Lust/Effects/fart.ogg");

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        // Запрещаем расстёгивание
        SubscribeLocalEvent<KabanizatorVictimComponent, UnbuckleAttemptEvent>(OnUnbuckleAttempt);
        // Запрещаем тащить свинью
        SubscribeLocalEvent<KabanizatorVictimComponent, BeingPulledAttemptEvent>(OnBeingPulledAttempt);
        // Запрещаем падать
        SubscribeLocalEvent<KabanizatorVictimComponent, DownAttemptEvent>(OnDownAttempt);
    }

    /// <summary>
    /// Блокирует любые попытки отстегнуть свинью со столба костра.
    /// </summary>
    private void OnUnbuckleAttempt(Entity<KabanizatorVictimComponent> ent, ref UnbuckleAttemptEvent args)
    {
        args.Cancelled = true;
    }

    /// <summary>
    /// Блокирует попытки перетаскивать или тянуть свинью.
    /// </summary>
    private void OnBeingPulledAttempt(Entity<KabanizatorVictimComponent> ent, ref BeingPulledAttemptEvent args)
    {
        args.Cancel();
    }

    /// <summary>
    /// Запрещает свинье падать на землю при получении оглушения или урона.
    /// </summary>
    private void OnDownAttempt(Entity<KabanizatorVictimComponent> ent, ref DownAttemptEvent args)
    {
        args.Cancel();
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<KabanizatorVictimComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var victim, out var xform))
        {
            // 1. Вращаем свинью на вертеле
            var deltaDeg = victim.SpinSpeed * frameTime;
            var newAngle = xform.LocalRotation.Degrees + deltaDeg;
            _transform.SetLocalRotation(uid, Angle.FromDegrees(newAngle), xform);

            // 2. Хрюканье
            if (curTime >= victim.NextOinkTime)
            {
                victim.NextOinkTime = curTime + victim.OinkInterval;
                _audio.PlayPvs(OinkSound, uid, AudioParams.Default.WithVolume(1.8f));
                _popup.PopupEntity(Loc.GetString("admin-smite-kabanizator-oink"), uid, PopupType.SmallCaution);
            }

            // 3. Пуканье
            if (curTime >= victim.NextFartTime)
            {
                victim.NextFartTime = curTime + victim.FartInterval;
                _audio.PlayPvs(FartSound, uid, AudioParams.Default.WithVolume(2.5f));
                _popup.PopupEntity(Loc.GetString("admin-smite-kabanizator-fart"), uid, PopupType.MediumCaution);
            }
        }
    }
}
