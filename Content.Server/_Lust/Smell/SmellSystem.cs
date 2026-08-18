using System.Linq;
using Content.Server.Popups;
using Content.Shared._Lust.Smell;
using Content.Shared._Lust.Smell.Components;
using Content.Shared._Lust.Smell.Prototypes;
using Content.Shared.ActionBlocker;
using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Verbs;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Lust.Smell;

/// <summary>
/// Система «нюханья»: верб «понюхать», проверки доступа, ленивый пересчёт временных
/// запахов и вывод читаемого описания. Наделение запахами (источники) живёт в
/// ScentAcquisitionSystem, общий кэш прототипов — в SmellPrototypeCacheSystem.
/// </summary>
public sealed class SmellSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SmellPrototypeCacheSystem _cache = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    /// <summary>
    /// Прототип запаха возбуждения — для него текст зависит от притяжения пары.
    /// </summary>
    private const string ArousalScent = "Arousal";

    /// <summary>
    /// Цвет текста, когда основной запах цели скрыт маскировкой (после мытья мылом).
    /// </summary>
    private const string MaskedScentColor = "#a6d8ff";

    public override void Initialize()
    {
        SubscribeLocalEvent<ScentComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }

    private void OnGetInteractionVerbs(
        Entity<ScentComponent> target,
        ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!HasComp<SmellComponent>(args.User))
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
        public string Voice { get; init; } = string.Empty;
    }
    public bool TrySmell(EntityUid user, Entity<ScentComponent> target)
    {
        if (!CanSmell(user, target))
        {
            // Показать причину, если запах не уловить из-за экипировки.
            if (IsMaskEquipped(user) || IsHardsuitSealed(user))
            {
                _popupSystem.PopupEntity(Loc.GetString("smell-blocked-by-gear"), user, user);
            }
            else if (IsHardsuitSealed(target))
            {
                _popupSystem.PopupEntity(Loc.GetString("smell-blocked-by-target-gear"), user, user);
            }

            return false;
        }

        DoSmell(user, target);
        return true;
    }

    public bool CanSmell(EntityUid user, Entity<ScentComponent> target)
    {
        if (!HasComp<SmellComponent>(user))
        {
            return false;
        }

        // Нюхающий в маске (не опущенной) или с закрытым шлемом — нюхать не может.
        if (IsMaskEquipped(user) || IsHardsuitSealed(user))
            return false;

        // Цель в герметичном скафандре с закрытым шлемом — её запах не почувствовать.
        if (IsHardsuitSealed(target))
            return false;

        if (!_actionBlocker.CanInteract(user, target))
            return false;

        return _interaction.InRangeUnobstructed(user, target.Owner);
    }

    /// <summary>
    /// Есть ли надетый и не опущенный предмет в слоте маски. Опущенная маска
    /// (MaskComponent.IsToggled) не закрывает нос и нюхать не мешает.
    /// </summary>
    private bool IsMaskEquipped(EntityUid uid)
    {
        if (!_inventory.TryGetSlotEntity(uid, "mask", out var maskEntity))
            return false;

        return TryComp<MaskComponent>(maskEntity, out var mask)
            && !mask.IsToggled;
    }

    /// <summary>
    /// Носит ли сущность герметичный скафандр с закрытым шлемом. Проверяем, что
    /// на слоте outerClothing есть скафандр (ToggleableClothing), а в слоте head
    /// надет его шлем (AttachedClothing, ссылающийся на этот скафандр).
    /// </summary>
    private bool IsHardsuitSealed(EntityUid uid)
    {
        if (!_inventory.TryGetSlotEntity(uid, "outerClothing", out var suitEntity))
            return false;

        // Без скафандра шлем-партнёр бессмысленен: он лишь отключает шлем.
        if (!HasComp<ToggleableClothingComponent>(suitEntity))
            return false;

        if (!_inventory.TryGetSlotEntity(uid, "head", out var helmetEntity))
            return false;

        return TryComp<AttachedClothingComponent>(helmetEntity, out var attached)
            && attached.AttachedUid == suitEntity;
    }

    private void DoSmell(EntityUid user, Entity<ScentComponent> target)
    {
        FormattedMessage message = new();

        // Ленивое снятие маскировки: если время истекло — маска пропадает сама.
        if (IsMasked(target))
        {
            message.AddMarkupOrThrow($"[color={MaskedScentColor}]{Loc.GetString("smell-result-masked")}[/color]");
        }
        else
        {
            List<string> staticNotes = [];

            foreach (ProtoId<ScentPrototype> scentId in target.Comp.BaseScents)
            {
                ScentPrototype scent = _prototypes.Index<ScentPrototype>(scentId);
                staticNotes.Add(GetScentDescription(scent));
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

    /// <summary>
    /// Активна ли временная маска. Если время истекло — маска снимается лениво
    /// (при очередном нюхании) и считается неактивной.
    /// </summary>
    private bool IsMasked(Entity<ScentComponent> target)
    {
        if (!target.Comp.Masked)
            return false;

        if (_timing.CurTime >= target.Comp.MaskUntil)
        {
            target.Comp.Masked = false;
            return false;
        }

        return true;
    }

    private ScentSignature? GetPersonalSignature(Entity<ScentComponent> target)
    {
        if (target.Comp.PersonalScentProfile is not { } profileId)
            return null;


        PersonalScentProfilePrototype profile =
            _prototypes.Index<PersonalScentProfilePrototype>(profileId);

        string name = Name(target.Owner) ?? "unknown";

        PersonalCharacteristics? characteristics = null;

        if (TryComp<HumanoidProfileComponent>(target.Owner, out HumanoidProfileComponent? appearanceComponent))
        {
            string voice = string.Empty;
            if (TryComp<TTSComponent>(target.Owner, out var tts))
                voice = tts.VoicePrototypeId?.ToString() ?? string.Empty;

            characteristics = new PersonalCharacteristics
            {
                Age = appearanceComponent.Age,
                Gender = appearanceComponent.Gender,
                Voice = voice,
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
            var scentProto = _prototypes.Index<ScentPrototype>(entry.Scent);
            result.Add((GetScentStrength(ratio), scentProto.Intensity, GetTemporaryScentText(user, target, entry)));
        }

        // Запахи состояний (пьянство, наркотрип): проверяются лениво по активным
        // статус-эффектам носителя. Сила — по положению внутри времени эффекта.
        AddStatusScents(target, result);

        // Свежие (сильные) группы раньше, внутри группы — по убыванию интенсивности.
        result.Sort((a, b) =>
        {
            int cmp = b.group.CompareTo(a.group);
            return cmp != 0 ? cmp : b.intensity.CompareTo(a.intensity);
        });

        return result;
    }

    /// <summary>
    /// Для каждого статус-запаха из YAML проверяет, активен ли соответствующий
    /// статус-эффект у носителя, и добавляет запах. Сила (Strong/Medium/Faint) —
    /// по положению внутри длительности эффекта: чем дальше до конца, тем сильнее.
    /// Нормируем по реальной длительности эффекта, а не по фиксированному порогу.
    /// </summary>
    private void AddStatusScents(Entity<ScentComponent> target, List<(ScentStrength group, float intensity, string text)> result)
    {
        var now = _timing.CurTime;

        // У цели нет контейнера статус-эффектов -> ничего не проверяем (иначе TryGetTime
        // логирует ошибку Resolve на каждую итерацию для сущностей без StatusEffectContainer).
        if (!HasComp<StatusEffectContainerComponent>(target))
            return;

        foreach (var proto in _cache.StatusScentProtos)
        {
            if (!_statusEffects.TryGetTime(target, proto.StatusEffect, out var time))
                continue;

            // Эффект без конечного времени считается длящимся бесконечно -> полная сила.
            if (time.EndEffectTime is not { } endTime)
            {
                var scentEndless = _prototypes.Index<ScentPrototype>(proto.Scent);
                result.Add((ScentStrength.Strong, scentEndless.Intensity, GetScentDescription(scentEndless)));
                continue;
            }

            var remaining = endTime - now;
            if (remaining <= TimeSpan.Zero)
                continue; // эффект уже фактически истёк.

            // Длительность эффекта; если она неизвестна (0), считаем эффект сильным.
            var total = endTime - time.StartEffectTime!.Value;
            if (total <= TimeSpan.Zero)
            {
                var scentFullyStrong = _prototypes.Index<ScentPrototype>(proto.Scent);
                result.Add((ScentStrength.Strong, scentFullyStrong.Intensity, GetScentDescription(scentFullyStrong)));
                continue;
            }

            // Чем больше осталось до конца, тем выше ratio (0 = конец, 1 = начало).
            var ratio = (float) Math.Clamp(remaining.TotalSeconds / total.TotalSeconds, 0.0, 1.0);
            var scent = _prototypes.Index<ScentPrototype>(proto.Scent);
            var strength = GetScentStrength(1f - ratio);

            // Короткий эффект не должен вонять сильно: его максимум — Medium, и чем короче,
            // тем слабее даже на пике (плавное затухание от Strong к Medium по длительности).
            if (proto.MinDurationForStrong > TimeSpan.Zero)
            {
                var durationScale = (float) Math.Clamp(
                    total.TotalSeconds / proto.MinDurationForStrong.TotalSeconds, 0.0, 1.0);
                if (strength == ScentStrength.Strong && durationScale < 1f)
                    strength = durationScale >= 0.5f ? ScentStrength.Medium : ScentStrength.Faint;
            }

            result.Add((strength, scent.Intensity, GetScentDescription(scent)));
        }
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
            return GetScentDescription(scent, attractive
                ? "scent-temp-arousal-attractive"
                : "scent-temp-arousal-plain");
        }

        return GetScentDescription(scent);
    }

    /// <summary>
    /// Возвращает локализованное описание запаха, обёрнутое в жирный шрифт,
    /// если у прототипа запаха стоит настройка Fat (акцентный/резкий запах).
    /// </summary>
    private string GetScentDescription(ScentPrototype scent, LocId? descriptionOverride = null)
    {
        var text = Loc.GetString(descriptionOverride ?? scent.Description);
        if (scent.Color is { } color)
            text = $"[color={color.ToHex()}]{text}[/color]";
        return scent.Fat ? $"[bold]{text}[/bold]" : text;
    }

    /// <summary>
    /// Определяет притяжение по формуле: Gender(нюхающий) x Sex(носитель).
    /// Футари трактуется как самец по запаху тела (см. вариант B).
    /// </summary>
    private bool IsAttractive(EntityUid smeller, EntityUid bearer)
    {
        if (!TryComp<HumanoidProfileComponent>(smeller, out var smellerHumanoid) ||
            !TryComp<HumanoidProfileComponent>(bearer, out var bearerHumanoid))
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
