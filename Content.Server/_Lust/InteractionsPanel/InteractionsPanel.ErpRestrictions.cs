using Content.Shared._Lust.ErpStatus;
using Content.Shared._Sunrise.InteractionsPanel.Data.Components;
using Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;
using Content.Shared._Sunrise.InteractionsPanel.Data.Prototypes;
using Content.Shared.Humanoid;

namespace Content.Server._Sunrise.InteractionsPanel;

public partial class InteractionsPanel
{
    // Зоны, взаимодействия которые пропадают при отклюенном ЕРП статусе
    private static readonly HashSet<string> RestrictedInteractionZones = new()
    {
        "грудь",
        "пах",
        "яйца",
        "член",
        "вагина",
        "анал",
        "попа",
        "рот",
        "ляжки"
    };

    private bool HasTargetErpLock(EntityUid user, EntityUid target)
    {
        if (user == target)
            return false;

        if (!TryComp<ErpStatusComponent>(target, out var status))
            return false;    

        // Для статуса «Без ERP» запрет действует автоматически,
        // независимо от значения галочки.
        if (status.Erp == Erp.No)
            return true;

        // При «Полном ERP» дополнительный запрет не применяется.
        if (status.Erp != Erp.Ask)
            return false;

        // При «Неполном ERP» запрет управляется галочкой.
        return TryComp<InteractionsComponent>(target, out var interactions)
            && interactions.LockErpInteractions;
    }

    private bool IsInteractionBlockedByZones(
        EntityUid user,
        EntityUid target,
        InteractionPrototype interaction)
    {
        if (!HasTargetErpLock(user, target))
            return false;

        foreach (var condition in interaction.AppearConditions)
        {
            if (ReferencesRestrictedTargetZone(condition))
                return true;
        }

        return false;
    }

    private bool ReferencesRestrictedTargetZone(
        IInteractionCondition condition)
    {
        switch (condition)
        {
            case BodyAreaTagCondition bodyArea:
            {
                if (!bodyArea.CheckTarget)
                    return false;

                foreach (var category in bodyArea.Categories)
                {
                    if (RestrictedInteractionZones.Contains(category))
                        return true;
                }

                return false;
            }

            case GenitalCondition genital:
                // Некоторые действия указывают орган через GenitalCondition,
                // а не через BodyAreaTagCondition.
                return genital.CheckTarget
                    && IsRestrictedGenital(genital.RequiredGenital);

            case InteractionAndCondition andCondition:
                foreach (var nested in andCondition.Conditions)
                {
                    if (ReferencesRestrictedTargetZone(nested))
                        return true;
                }

                return false;

            case InteractionOrCondition orCondition:
                // Консервативно запрещаем всё действие, если хотя бы
                // одна его ветка ссылается на запрещённую зону цели.
                foreach (var nested in orCondition.Conditions)
                {
                    if (ReferencesRestrictedTargetZone(nested))
                        return true;
                }

                return false;

            case InteractionNotCondition notCondition:
                return ReferencesRestrictedTargetZone(
                    notCondition.Condition);

            default:
                return false;
        }
    }

    private bool IsRestrictedGenital(GenitalSlot slot)
    {
        return slot is GenitalSlot.Penis
            or GenitalSlot.Vagina
            or GenitalSlot.Boobs
            or GenitalSlot.Anus;
    }
}