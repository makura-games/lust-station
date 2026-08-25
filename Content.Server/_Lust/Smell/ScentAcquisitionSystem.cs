using Content.Shared._Lust.Smell;
using Content.Shared._Lust.Smell.Components;
using Content.Shared._Lust.Smell.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Implants;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Lust.Smell;

/// <summary>
/// Scent source event handler system: reacts to events (ERP, damage,
/// ScentEmitter, finishing off a critical target) and records the acquired scent
/// into the bearer's ScentComponent via AddTemporaryScent.
/// </summary>
public sealed class ScentAcquisitionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SmellPrototypeCacheSystem _cache = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {

        SubscribeLocalEvent<ArousalStartedEvent>(OnArousalStarted);

        SubscribeLocalEvent<OrgasmPerformedEvent>(OnOrgasmPerformed);


        SubscribeLocalEvent<ScentEmitterComponent, GotEquippedEvent>(OnScentEmitterEquipped);

        SubscribeLocalEvent<ScentEmitterComponent, GotEquippedHandEvent>(OnScentEmitterPickedUp);

        SubscribeLocalEvent<ScentEmitterComponent, ImplantImplantedEvent>(OnScentEmitterImplanted);

        SubscribeLocalEvent<ScentComponent, DamageChangedEvent>(OnDamageChanged);

        SubscribeLocalEvent<ScentOnAttackedComponent, AttackedEvent>(OnAttacked);
    }
    /// <summary>
    /// Adds a temporary arousal scent.
    /// </summary>
    private void OnArousalStarted(ArousalStartedEvent args)
    {
        AddTemporaryScent(args.Uid, ScentIds.Arousal, _cache.Config.ArousalScentDuration);
    }

    /// <summary>
    /// Adds a temporary orgasm scent.
    /// </summary>
    private void OnOrgasmPerformed(OrgasmPerformedEvent args)
    {
        AddTemporaryScent(args.User, ScentIds.Orgasm, _cache.Config.OrgasmScentDuration);
        // проверка нужна, так как эвент наделяет запахом обоих участников ерп, а не только того,
        // у кого произошёл оргазм; поэтому проверка не даёт задвоить запах, если ерп занимался лишь игрок сам с собой
        if (args.Target != args.User)
            AddTemporaryScent(args.Target, ScentIds.Orgasm, _cache.Config.OrgasmScentDuration);
    }

    /// <summary>
    /// Scent-emitting item equipped into a clothing slot. Reacts to SpecificSlot
    /// (checks the required slot) and AnySlot modes. Hands mode is skipped here.
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
    /// Picking up a scent-emitting item into a hand: fires for Hands and AnySlot modes.
    /// </summary>
    private void OnScentEmitterPickedUp(Entity<ScentEmitterComponent> ent, ref GotEquippedHandEvent args)
    {
        if (ent.Comp.Spot != ScentEmitSpot.Hands && ent.Comp.Spot != ScentEmitSpot.AnySlot)
            return;

        AddTemporaryScent(args.User, ent.Comp.Scent, ent.Comp.Duration);
    }

    /// <summary>
    /// Injecting an implant that carries a scent grants the recipient
    /// a temporary scent dose. The implanted device itself no longer emits:
    /// skin and the implant shell hide it.
    /// </summary>
    private void OnScentEmitterImplanted(Entity<ScentEmitterComponent> ent, ref ImplantImplantedEvent args)
    {
        AddTemporaryScent(args.Implanted, ent.Comp.Scent, ent.Comp.Duration);
    }

    /// <summary>
    /// Checks incoming damage and adds three scents: blood (slashes and piercings),
    /// poison and bruises.
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

        if (cuts > _cache.Config.WoundScentThreshold)
            AddTemporaryScent((ent.Owner, ent.Comp), ScentIds.Blood, _cache.Config.WoundScentDuration);

        if (dict.TryGetValue("Blunt", out var blunt) && blunt > _cache.Config.WoundScentThreshold)
            AddTemporaryScent((ent.Owner, ent.Comp), ScentIds.Bruise, _cache.Config.WoundScentDuration);

        if (dict.TryGetValue("Poison", out var poison) && poison > _cache.Config.PoisonScentThreshold)
            AddTemporaryScent((ent.Owner, ent.Comp), ScentIds.Poison, _cache.Config.PoisonScentDuration);
    }

    /// <summary>
    /// Adds the killer's scent when damaging a critical-state target.
    /// Works with any melee weapon (crowbar, knife, etc.); the owner is the event User.
    /// Repeated hits simply refresh the single scent's timer (AddTemporaryScent
    /// replaces instead of duplicating).
    /// </summary>
    private void OnAttacked(Entity<ScentOnAttackedComponent> ent, ref AttackedEvent args)
    {
        if (!_mobState.IsCritical(ent.Owner))
        {
            return;
        }

        if (!HasComp<ScentComponent>(args.User))
            return;

        AddTemporaryScent(args.User, ScentIds.OtherBlood, _cache.Config.OtherBloodScentDuration);
    }

    /// <summary>
    /// Public API for scent sources (chemistry, smoking, ERP): add a temporary scent.
    /// Re-applying the same scent replaces the entry instead of duplicating it.
    /// </summary>
    public void AddTemporaryScent(Entity<ScentComponent?> ent, ProtoId<ScentPrototype> scent, TimeSpan duration)
    {

        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var entry = new ActiveTemporaryScent
        {
            Scent = scent,
            StartTime = _timing.CurTime,
            Duration = duration,
        };

        for (int i = 0; i < ent.Comp.TemporaryScents.Count; i++)
        {
            if (ent.Comp.TemporaryScents[i].Scent == scent)
            {
                ent.Comp.TemporaryScents[i] = entry;
                return;
            }
        }

        ent.Comp.TemporaryScents.Add(entry);
    }
}
