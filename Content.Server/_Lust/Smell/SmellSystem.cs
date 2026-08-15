using System.Linq;
using Content.Shared._Lust.Smell;
using Content.Shared._Lust.Smell.Components;
using Content.Shared._Lust.Smell.Prototypes;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Verbs;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Lust.Smell;

public sealed class SmellSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Прототип запаха возбуждения — для него текст зависит от притяжения пары.
    /// </summary>
    private const string ArousalScent = "Arousal";

    /// <summary>
    /// Минимальный накопленный урон конкретного типа, чтобы существо источало запах
    /// своих ран (меньше — микро-царапины, раной не пахнет).
    /// </summary>
    private const int WoundScentThreshold = 10;

    /// <summary>
    /// Параметры временного запаха от ран.
    /// </summary>
    private static readonly TimeSpan WoundScentDuration = TimeSpan.FromSeconds(300);
    private const float WoundScentIntensity = 0.8f;

    /// <summary>
    /// Сопоставление trigger -> ScentEventPrototype, собранное один раз из YAML.
    /// </summary>
    private readonly Dictionary<string, ScentEventPrototype> _eventProtoIndex = new();

    public override void Initialize()
    {
        // Собираем индекс всех событие->запах из прототипов (гибко: новые источники в YAML,
        // а не в коде). Последний прототип с одинаковым trigger побеждает.
        foreach (var proto in _prototypes.EnumeratePrototypes<ScentEventPrototype>())
        {
            _eventProtoIndex[proto.Trigger] = proto;
        }

        SubscribeLocalEvent<ScentComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);

        // Слушаем интимные взаимодействия, поднятые ERP-панелью, и применяем временные запахи.
        SubscribeLocalEvent<ErpInteractionPerformedEvent>(OnErpInteractionPerformed);

        // Эмиторы запаха: предмет попал в нужный слот -> носитель получает запах.
        SubscribeLocalEvent<ScentEmitterComponent, GotEquippedEvent>(OnScentEmitterEquipped);

        // Взятие в руки тоже активирует эмитор (закрывает карманы/рюкзак: положить
        // куда-либо предмет можно только взяв его в руки).
        SubscribeLocalEvent<ScentEmitterComponent, GotEquippedHandEvent>(OnScentEmitterPickedUp);

        // Урон: существо пахнет кровью/ушибами собственных ран.
        SubscribeLocalEvent<ScentComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnErpInteractionPerformed(ErpInteractionPerformedEvent args)
    {
        if (string.IsNullOrEmpty(args.Trigger))
            return;

        // Ищем прототип-сопоставление по ключу события.
        if (!_eventProtoIndex.TryGetValue(args.Trigger, out var eventProto))
            return;

        switch (eventProto.ApplyTo)
        {
            case ScentApplyTarget.Self:
                TryApplyErpScent(args.User, eventProto);
                break;
            case ScentApplyTarget.User:
                TryApplyErpScent(args.User, eventProto);
                break;
            case ScentApplyTarget.Target:
                TryApplyErpScent(args.Target, eventProto);
                break;
            case ScentApplyTarget.Both:
                TryApplyErpScent(args.User, eventProto);
                TryApplyErpScent(args.Target, eventProto);
                break;
        }
    }

    private void TryApplyErpScent(EntityUid uid, ScentEventPrototype proto)
    {
        AddTemporaryScent(uid, proto.Scent, proto.Duration, proto.Intensity);
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

        AddTemporaryScent(args.Equipee, ent.Comp.Scent, ent.Comp.Duration, ent.Comp.Intensity);
    }

    /// <summary>
    /// Поднятие в руку: срабатывает для режимов Hands и AnySlot.
    /// AnySlot нужен, чтобы предмет пах, лежа и просто в руке/кармане (не только в слотах одежды).
    /// </summary>
    private void OnScentEmitterPickedUp(EntityUid uid, ScentEmitterComponent comp, GotEquippedHandEvent args)
    {
        if (comp.Spot != ScentEmitSpot.Hands && comp.Spot != ScentEmitSpot.AnySlot)
            return;

        AddTemporaryScent(args.User, comp.Scent, comp.Duration, comp.Intensity);
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
            AddTemporaryScent(ent, "Blood", WoundScentDuration, WoundScentIntensity);
        }

        // Тупые удары оставляют синяки.
        if (dict.TryGetValue("Blunt", out var blunt) && blunt > WoundScentThreshold)
            AddTemporaryScent(ent, "Bruise", WoundScentDuration, WoundScentIntensity);
    }


    private void OnGetInteractionVerbs(
        Entity<ScentComponent> target,
        ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<SmellComponent>(args.User, out SmellComponent? smell)
            || smell.SmellBlocked)
        {
            return;
        }
        EntityUid user = args.User;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("smell-verb"),
            TextStyleClass = "Default",
            Act = () => TrySmell(user, target)
        });
    }

    public sealed record PersonalCharacteristics
    {
        public int Age { get; init; }
        public Gender Gender { get; init; }
        public string? Voice { get; init; }
    }
    public bool TrySmell(EntityUid user, Entity<ScentComponent> target)
    {
        if (!CanSmell(user, target))
            return false;

        DoSmell(user, target);
        return true;
    }

    /// <summary>
    /// Публичное API для источников (химия, курение, ERP): добавить временный запах.
    /// Ленивая загрузка: только кладём запись, протухание вычисляем при запросе.
    /// </summary>
    public void AddTemporaryScent(EntityUid uid, ProtoId<ScentPrototype> scent, TimeSpan duration, float intensity = 1f)
    {
        if (!TryComp<ScentComponent>(uid, out var scentComponent))
            return;

        // Перезапись: обновляем свежим появлением вместо дублирования одинаковых запахов.
        for (int i = 0; i < scentComponent.TemporaryScents.Count; i++)
        {
            if (scentComponent.TemporaryScents[i].Scent == scent)
            {
                scentComponent.TemporaryScents[i] = new ActiveTemporaryScent
                {
                    Scent = scent,
                    StartTime = _timing.CurTime,
                    Duration = duration,
                    Intensity = intensity,
                };
                return;
            }
        }

        scentComponent.TemporaryScents.Add(new ActiveTemporaryScent
        {
            Scent = scent,
            StartTime = _timing.CurTime,
            Duration = duration,
            Intensity = intensity,
        });
    }

    public bool CanSmell(EntityUid user, Entity<ScentComponent> target)
    {
        if (!TryComp<SmellComponent>(user, out SmellComponent? smell)
            || smell.SmellBlocked)
        {
            return false;
        }

        if (!_actionBlocker.CanInteract(user, target))
            return false;

        return _interaction.InRangeUnobstructed(user, target.Owner);
    }

    private void DoSmell(EntityUid user, Entity<ScentComponent> target)
    {
        FormattedMessage message = new();
        List<string> staticNotes = [];

        foreach (ProtoId<ScentPrototype> scentId in target.Comp.BaseScents)
        {
            ScentPrototype scent = _prototypes.Index<ScentPrototype>(scentId);
            staticNotes.Add(Loc.GetString(scent.Description));
        }

        ScentSignature? signature = GetPersonalSignature(target);

        // --- ОСНОВНОЙ запах (статичный + личный) всегда в начале ---
        if (staticNotes.Count > 0)
        {
            message.AddMarkupOrThrow(Loc.GetString(
                "smell-result-static",
                ("notes", string.Join(", ", staticNotes))));
        }

        if (signature != null)
        {
            if (staticNotes.Count > 0)
                message.AddMarkupOrThrow("\n");

            List<string> personalNotes = [];

            foreach (LocId note in signature.Notes)
            {
                personalNotes.Add(Loc.GetString(note));
            }

            message.AddMarkupOrThrow(Loc.GetString(
                "smell-result-personal",
                ("color", signature.Color.ToHex()),
                ("notes", string.Join(", ", personalNotes))));
        }

        if (staticNotes.Count == 0 && signature == null)
        {
            message.AddMarkupOrThrow(Loc.GetString("smell-result-none"));
        }

        // --- Временные запахи: ленивый пересчёт по возрасту, выводятся ниже основного ---
        List<(ScentStrength group, float intensity, string text)> tempNotes = GetTemporaryScentNotes(user, target);

        if (tempNotes.Count > 0)
        {
            message.AddMarkupOrThrow("\n");
            message.AddMarkupOrThrow(Loc.GetString("smell-result-temporary-header"));

            // Отдельная строка на каждую непустую группу, в порядке Strong -> Medium -> Faint.
            foreach (ScentStrength group in Enum.GetValues<ScentStrength>())
            {
                var groupLines = tempNotes
                    .Where(n => n.group == group)
                    .Select(n => n.text)
                    .ToList();

                if (groupLines.Count == 0)
                    continue;

                message.AddMarkupOrThrow("\n");
                message.AddMarkupOrThrow(Loc.GetString(
                    $"smell-strength-{group.ToString().ToLowerInvariant()}",
                    ("notes", string.Join(", ", groupLines))));
            }
        }

        _examine.SendExamineTooltip(user, target, message, false, false);
    }

    private ScentSignature? GetPersonalSignature(Entity<ScentComponent> target)
    {
        if (target.Comp.PersonalScentProfile is not { } profileId)
            return null;


        PersonalScentProfilePrototype profile =
            _prototypes.Index<PersonalScentProfilePrototype>(profileId);

        string name = Name(target.Owner) ?? "unknown";

        PersonalCharacteristics? characteristics = null;

        if (TryComp<HumanoidAppearanceComponent>(target.Owner, out HumanoidAppearanceComponent? appearanceComponent))
        {
            characteristics = new PersonalCharacteristics
            {
                Age = appearanceComponent.Age,
                Gender = appearanceComponent.Gender,
                Voice = appearanceComponent.Voice,
            };
        }


        string seed = $"{name}";
        if (characteristics != null)
        {
            seed += $":{characteristics.Age}:{characteristics.Gender}:{characteristics.Voice}";
        }

        return ScentSignatureGenerator.Generate(seed, profile);
    }

    /// <summary>
    /// Лениво пересчитывает временные запахи: отбрасывает протухшие,
    /// определяет группу силы по возрасту и сортирует по интенсивности.
    /// </summary>
    private List<(ScentStrength group, float intensity, string text)> GetTemporaryScentNotes(
        EntityUid user, Entity<ScentComponent> target)
    {
        var result = new List<(ScentStrength group, float intensity, string text)>();
        var now = _timing.CurTime;

        // Проходим с конца, чтобы безопасно удалять мёртвые записи.
        for (int i = target.Comp.TemporaryScents.Count - 1; i >= 0; i--)
        {
            var entry = target.Comp.TemporaryScents[i];

            // Защита от деления на ноль и от отрицательной длительности.
            if (entry.Duration <= TimeSpan.Zero)
            {
                target.Comp.TemporaryScents.RemoveAt(i);
                continue;
            }

            // Протух: убираем за ненадобностью (часть ленивой очистки).
            var lifetime = entry.StartTime + entry.Duration;
            if (lifetime <= now)
            {
                target.Comp.TemporaryScents.RemoveAt(i);
                continue;
            }

            var age = now - entry.StartTime;
            var ratio = (float) (age / entry.Duration);
            result.Add((GetScentStrength(ratio), entry.Intensity, GetTemporaryScentText(user, target, entry)));
        }

        // Свежие (сильные) группы раньше, внутри группы — по убыванию интенсивности.
        result.Sort((a, b) =>
        {
            int cmp = b.group.CompareTo(a.group);
            return cmp != 0 ? cmp : b.intensity.CompareTo(a.intensity);
        });

        return result;
    }

    /// <summary>
    /// Возвращает текст временного запаха. Для запаха возбуждения выбирает
    /// вариант в зависимости от притяжения нюхающего (user) к носителю (target).
    /// </summary>
    private string GetTemporaryScentText(EntityUid user, Entity<ScentComponent> target, ActiveTemporaryScent entry)
    {
        var scent = _prototypes.Index<ScentPrototype>(entry.Scent);

        // У Arousal одно и то же тело пахнет по-разному в зависимости от пары.
        if (entry.Scent == ArousalScent)
        {
            bool attractive = IsAttractive(user, target.Owner);
            return Loc.GetString(attractive
                ? "scent-temp-arousal-attractive"
                : "scent-temp-arousal-plain");
        }

        return Loc.GetString(scent.Description);
    }

    /// <summary>
    /// Определяет притяжение по формуле: Gender(нюхающий) x Sex(носитель).
    /// Футари трактуется как самец по запаху тела (см. вариант B).
    /// </summary>
    private bool IsAttractive(EntityUid smeller, EntityUid bearer)
    {
        if (!TryComp<HumanoidAppearanceComponent>(smeller, out var smellerHumanoid) ||
            !TryComp<HumanoidAppearanceComponent>(bearer, out var bearerHumanoid))
            return false;

        return smellerHumanoid.Gender switch
        {
            // Женский нюх -> мужское тело притягивает (вкл. футари).
            Gender.Female => bearerHumanoid.Sex is Sex.Male or Sex.Futanari,
            // Мужской нюх -> женское тело притягивает.
            Gender.Male   => bearerHumanoid.Sex is Sex.Female,
            // Epicene/Neuter -> нейтрально.
            _             => false,
        };
    }

    /// <summary>
    /// Определяет группу силы по доле прожитого времени (0 = только появился, 1 = почти истёк).
    /// </summary>
    private static ScentStrength GetScentStrength(float ratio)
    {
        if (ratio < 0.33f) return ScentStrength.Strong;
        if (ratio < 0.66f) return ScentStrength.Medium;
        return ScentStrength.Faint;
    }
}

/// <summary>
/// Три группы силы запаха, используемые для сортировки при описании.
/// </summary>
public enum ScentStrength
{
    Strong,
    Medium,
    Faint,
}
