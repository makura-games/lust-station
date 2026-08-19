using Content.Shared._Lust.Smell;
using Content.Shared._Lust.Smell.Components;
using Content.Shared._Lust.Smell.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Lust.Smell;

/// <summary>
/// Источник временных запахов: реагирует на события (ERP, урон, эмиторы на предметах,
/// добивание) и записывает полученный запах в ScentComponent носителя через
/// AddTemporaryScent. Протухание временных запахов вычисляется лениво при чтении
/// в SmellSystem, поэтому здесь мусор не чистится. Чтение и вывод запахов — в SmellSystem.
/// </summary>
public sealed class ScentAcquisitionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SmellPrototypeCacheSystem _smellCache = default!;

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
        AddTemporaryScent(args.Uid, ScentIds.Arousal, _smellCache.Config.ArousalScentDuration);
    }

    private void OnOrgasmPerformed(OrgasmPerformedEvent args)
    {
        AddTemporaryScent(args.User, ScentIds.Orgasm, _smellCache.Config.OrgasmScentDuration);
        if (args.Target != args.User)
            AddTemporaryScent(args.Target, ScentIds.Orgasm, _smellCache.Config.OrgasmScentDuration);
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
    private void OnScentEmitterPickedUp(EntityUid _, ScentEmitterComponent comp, GotEquippedHandEvent args)
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

        var dict = _damageable.GetAllDamage((ent.Owner, args.Damageable)).DamageDict;

        // Порезы и уколы оставляют открытые раны, пахнущие кровью.
        // Порог считаем по сумме Slash и Piercing: смесь мелких порезов
        // и проколов — тоже значимая открытая рана.
        FixedPoint2 cuts = FixedPoint2.Zero;
        if (dict.TryGetValue("Slash", out var slash))
            cuts += slash;
        if (dict.TryGetValue("Piercing", out var piercing))
            cuts += piercing;

        if (cuts > _smellCache.Config.WoundScentThreshold)
            AddTemporaryScent(ent, ScentIds.Blood, _smellCache.Config.WoundScentDuration);

        if (dict.TryGetValue("Blunt", out var blunt) && blunt > _smellCache.Config.WoundScentThreshold)
            AddTemporaryScent(ent, ScentIds.Bruise, _smellCache.Config.WoundScentDuration);

        if (dict.TryGetValue("Poison", out var poison) && poison > _smellCache.Config.PoisonScentThreshold)
            AddTemporaryScent(ent, ScentIds.Poison, _smellCache.Config.PoisonScentDuration);
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
        if (TryComp<MobStateComponent>(uid, out var mobState)
            && mobState.CurrentState != MobState.Critical)
        {
            return;
        }

        if (!HasComp<ScentComponent>(args.User))
            return;

        AddTemporaryScent(args.User, ScentIds.OtherBlood, _smellCache.Config.OtherBloodScentDuration);
    }

    /// <summary>
    /// Публичное API для источников (химия, курение, ERP): добавить временный запах.
    /// Ленивая загрузка: только кладём запись, удаление вычисляем при запросе.
    /// </summary>
    public void AddTemporaryScent(EntityUid uid, ProtoId<ScentPrototype> scent, TimeSpan duration)
    {
        if (!TryComp<ScentComponent>(uid, out var scentComponent))
            return;

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
