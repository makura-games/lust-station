using Content.Shared._Lust.Smell;
using Content.Shared._Lust.Smell.Components;
using Content.Shared._Lust.Smell.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Lust.Smell;

/// <summary>
/// Система обработки событий-источников запаха: реагирует на события (ERP, урон,
/// ScentEmitter, добивание критической цели) и записывает полученный запах
/// в ScentComponent носителя через AddTemporaryScent.
/// </summary>
public sealed class ScentAcquisitionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SmellPrototypeCacheSystem _smellCache = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {

        SubscribeLocalEvent<ArousalStartedEvent>(OnArousalStarted);

        SubscribeLocalEvent<OrgasmPerformedEvent>(OnOrgasmPerformed);


        SubscribeLocalEvent<ScentEmitterComponent, GotEquippedEvent>(OnScentEmitterEquipped);

        SubscribeLocalEvent<ScentEmitterComponent, GotEquippedHandEvent>(OnScentEmitterPickedUp);

        SubscribeLocalEvent<ScentComponent, DamageChangedEvent>(OnDamageChanged);

        SubscribeLocalEvent<ScentOnAttackedComponent, AttackedEvent>(OnAttacked);
    }
    /// <summary>
    /// Добавляет временный запах возбуждения.
    /// </summary>
    private void OnArousalStarted(ArousalStartedEvent args)
    {
        AddTemporaryScent(args.Uid, ScentIds.Arousal, _smellCache.Config.ArousalScentDuration);
    }

    /// <summary>
    /// Добавляет временный запах оргазма.
    /// </summary>
    private void OnOrgasmPerformed(OrgasmPerformedEvent args)
    {
        AddTemporaryScent(args.User, ScentIds.Orgasm, _smellCache.Config.OrgasmScentDuration);
        // проверка нужна, так как эвент наделяет запахом обоих участников ерп, а не только того,
        // у кого произошёл оргазм; поэтому проверка не даёт задвоить запах, если ерп занимался лишь игрок сам с собой
        if (args.Target != args.User)
            AddTemporaryScent(args.Target, ScentIds.Orgasm, _smellCache.Config.OrgasmScentDuration);
    }

    /// <summary>
    /// Функция для предмета-эмитора запаха. Проверяет, надет ли он в слот одежды. Реагирует на режимы
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
    /// Поднятие предмета эмитора запахов в руку: срабатывает для режимов Hands и AnySlot.
    /// </summary>
    private void OnScentEmitterPickedUp(Entity<ScentEmitterComponent> ent, ref GotEquippedHandEvent args)
    {
        if (ent.Comp.Spot != ScentEmitSpot.Hands && ent.Comp.Spot != ScentEmitSpot.AnySlot)
            return;

        AddTemporaryScent(args.User, ent.Comp.Scent, ent.Comp.Duration);
    }

    /// <summary>
    /// Функция для проверки полученного урона и добавления трёх запахов: от кровотечения
    /// (порезы и уколы), от ядов и от ушибов.
    /// </summary>
    private void OnDamageChanged(Entity<ScentComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        var dict = _damageable.GetAllDamage((ent.Owner, args.Damageable)).DamageDict;

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
    /// Функция добавляющая запах убийцы при нанесении урона по критической цели.
    /// Работает с любым melee-оружием (лом, нож и т.п.); владелец — User события.
    /// Повторные удары просто обновляют таймер одного запаха (AddTemporaryScent
    /// перезаписывает, не дублирует).
    /// </summary>
    private void OnAttacked(Entity<ScentOnAttackedComponent> ent, ref AttackedEvent args)
    {
        if (!_mobState.IsCritical(ent.Owner))
        {
            return;
        }

        if (!HasComp<ScentComponent>(args.User))
            return;

        AddTemporaryScent(args.User, ScentIds.OtherBlood, _smellCache.Config.OtherBloodScentDuration);
    }

    /// <summary>
    /// Публичное API для источников (химия, курение, ERP): добавить временный запах.
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
