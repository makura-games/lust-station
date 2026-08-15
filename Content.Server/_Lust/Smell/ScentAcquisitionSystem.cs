using Content.Shared._Lust.Smell;
using Content.Shared._Lust.Smell.Components;
using Content.Shared._Lust.Smell.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Lust.Smell;

/// <summary>
/// Система «наделения» временными запахами: собирает источники запахов (события жизненной
/// системы ERP, эмиторы на предметах, собственные раны, кровь при добивании) и записывает
/// их в носитель (ScentComponent). Сюда же позже ляжет периодическая/ленивая очистка мусора
/// (устаревшие временные запахи). Логика чтения и вывода запахов — в SmellSystem.
/// </summary>
public sealed class ScentAcquisitionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Порог накопленного урона, с которого существо начинает пахнуть (порезы/ушибы).
    /// </summary>
    private const int WoundScentThreshold = 10;

    /// <summary>
    /// Порог накопленного яда (Poison), с которого тело пахнет токсинами.
    /// </summary>
    private const int PoisonScentThreshold = 50;

    /// <summary>
    /// Длительность запаха от раны.
    /// </summary>
    private static readonly TimeSpan WoundScentDuration = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Запах «чужой крови», появляющийся у атакующего при добивании жертвы.
    /// </summary>
    private const string OtherBloodScent = "OtherBlood";

    /// <summary>
    /// Запах возбуждения на самом себе.
    /// </summary>
    private const string ArousalScent = "Arousal";

    /// <summary>
    /// Длительность запаха возбуждения.
    /// </summary>
    private static readonly TimeSpan ArousalScentDuration = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Запах оргазма.
    /// </summary>
    private const string OrgasmScent = "Orgasm";

    /// <summary>
    /// Длительность запаха оргазма (на себе и на партнёре).
    /// </summary>
    private static readonly TimeSpan OrgasmScentDuration = TimeSpan.FromSeconds(300);

    public override void Initialize()
    {
        // События жизненной системы: возбуждение началось, оргазм.
        SubscribeLocalEvent<ArousalStartedEvent>(OnArousalStarted);
        SubscribeLocalEvent<OrgasmPerformedEvent>(OnOrgasmPerformed);

        // Эмиторы запаха: предмет попал в нужный слот -> носитель получает запах.
        SubscribeLocalEvent<ScentEmitterComponent, GotEquippedEvent>(OnScentEmitterEquipped);

        // Взятие в руки тоже активирует эмитор (закрывает карманы/рюкзак: положить
        // куда-либо предмет можно только взяв его в руки).
        SubscribeLocalEvent<ScentEmitterComponent, GotEquippedHandEvent>(OnScentEmitterPickedUp);

        // Урон: существо пахнет кровью/ушибами собственных ран.
        SubscribeLocalEvent<ScentComponent, DamageChangedEvent>(OnDamageChanged);

        // Получение атаки ближнего боя по жертве в критическом состоянии: атакующему — запах
        // чужой крови. AttackedEvent рейзится направленно на жертве (broadcast=false), поэтому
        // подписка идёт на наш маркер ScentOnAttacked (наследуется от базового предка живых
        // MobDamageable), а не на общие пары вроде (MobStateComponent, AttackedEvent), чтобы
        // не занимать пару, которую может захотеть апстрим-контент. Владелец — args.User.
        SubscribeLocalEvent<ScentOnAttackedComponent, AttackedEvent>(OnAttacked);
    }

    private void OnArousalStarted(ArousalStartedEvent args)
    {
        AddTemporaryScent(args.Uid, ArousalScent, ArousalScentDuration);
    }

    private void OnOrgasmPerformed(OrgasmPerformedEvent args)
    {
        AddTemporaryScent(args.User, OrgasmScent, OrgasmScentDuration);
        if (args.Target != args.User)
            AddTemporaryScent(args.Target, OrgasmScent, OrgasmScentDuration);
    }

    /// <summary>
    /// Предмет-эмитор надет в слот одежды. Реагирует на режимы
    /// SpecificSlot (проверяет нужный слот) и AnySlot. Для Hands — пропускает.
    /// </summary>
    private void OnScentEmitterEquipped(Entity<ScentEmitterComponent> ent, ref GotEquippedEvent args)
    {
        switch (ent.Comp.Spot)
        {
            case ScentEmitSpot.Hands:
                return; // руки обрабатывает отдельное событие.
            case ScentEmitSpot.SpecificSlot:
                if (args.Slot != ent.Comp.Slot)
                    return;
                break;
            case ScentEmitSpot.AnySlot:
                break; // любой слот -> даём запах.
        }

        AddTemporaryScent(args.Equipee, ent.Comp.Scent, ent.Comp.Duration);
    }

    /// <summary>
    /// Поднятие в руку: срабатывает для режимов Hands и AnySlot.
    /// AnySlot нужен, чтобы предмет пах, лежа и просто в руке/кармане (не только в слотах одежды).
    /// </summary>
    private void OnScentEmitterPickedUp(EntityUid uid, ScentEmitterComponent comp, GotEquippedHandEvent args)
    {
        if (comp.Spot != ScentEmitSpot.Hands && comp.Spot != ScentEmitSpot.AnySlot)
            return;

        AddTemporaryScent(args.User, comp.Scent, comp.Duration);
    }

    /// <summary>
    /// Существо получило урон: собственные раны пахнут кровью, а ушибы — «синяками».
    /// Кровь — от порезов (Slash) и уколов (Piercing), синяк — от тупых ударов (Blunt).
    /// Реагируем только на значимый накопленный урон (порог), чтобы игнорировать мелочь.
    /// </summary>
    private void OnDamageChanged(Entity<ScentComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        var dict = args.Damageable.Damage.DamageDict;

        // Порезы и уколы оставляют открытые раны, пахнущие кровью.
        if ((dict.TryGetValue("Slash", out var slash) && slash > WoundScentThreshold)
            || (dict.TryGetValue("Piercing", out var piercing) && piercing > WoundScentThreshold))
        {
            AddTemporaryScent(ent, "Blood", WoundScentDuration);
        }

        // Тупые удары оставляют синяки.
        if (dict.TryGetValue("Blunt", out var blunt) && blunt > WoundScentThreshold)
            AddTemporaryScent(ent, "Bruise", WoundScentDuration);

        // Отравление: заметный накопленный яд даёт запах токсинов.
        if (dict.TryGetValue("Poison", out var poison) && poison > PoisonScentThreshold)
            AddTemporaryScent(ent, "Poison", WoundScentDuration);
    }

    /// <summary>
    /// Жертва получила атаку ближнего боя (AttackedEvent рейзится на ней). Если она уже
    /// в критическом состоянии — её добивают — атакующий получает запах чужой крови.
    /// Работает с любым melee-оружием (лом, нож и т.п.); владелец — User события.
    /// Повторные удары просто обновляют таймер одного запаха (AddTemporaryScent
    /// перезаписывает, не дублирует).
    /// </summary>
    private void OnAttacked(EntityUid uid, ScentOnAttackedComponent component, AttackedEvent args)
    {
        // Жертва должна быть в критическом состоянии (её бьют на грани смерти).
        if (TryComp<MobStateComponent>(uid, out var mobState)
            && mobState.CurrentState != MobState.Critical)
        {
            return;
        }

        // Запах получает только носитель запахов (вульпканины и пр.).
        if (!HasComp<ScentComponent>(args.User))
            return;

        AddTemporaryScent(args.User, OtherBloodScent, WoundScentDuration);
    }

    /// <summary>
    /// Публичное API для источников (химия, курение, ERP): добавить временный запах.
    /// Ленивая загрузка: только кладём запись, протухание вычисляем при запросе.
    /// </summary>
    public void AddTemporaryScent(EntityUid uid, ProtoId<ScentPrototype> scent, TimeSpan duration)
    {
        if (!TryComp<ScentComponent>(uid, out var scentComponent))
            return;

        // Перезапись: обновляем свежим появлением вместо дублирования одинаковых запахов.
        // Интенсивность единая для всех источников — берётся из прототипа самого запаха.
        for (int i = 0; i < scentComponent.TemporaryScents.Count; i++)
        {
            if (scentComponent.TemporaryScents[i].Scent == scent)
            {
                scentComponent.TemporaryScents[i] = new ActiveTemporaryScent
                {
                    Scent = scent,
                    StartTime = _timing.CurTime,
                    Duration = duration,
                };
                return;
            }
        }

        scentComponent.TemporaryScents.Add(new ActiveTemporaryScent
        {
            Scent = scent,
            StartTime = _timing.CurTime,
            Duration = duration,
        });
    }
}
