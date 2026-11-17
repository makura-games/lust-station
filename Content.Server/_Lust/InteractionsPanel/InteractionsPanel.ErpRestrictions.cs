using Content.Shared._Lust.ErpStatus;
using Content.Shared._Sunrise.InteractionsPanel.Data.Components;
using Content.Shared._Sunrise.InteractionsPanel.Data.Prototypes;
using Content.Shared.Humanoid;

namespace Content.Server._Sunrise.InteractionsPanel;

public partial class InteractionsPanel
{
    /// <summary>
    /// Проверяет, действует ли запрет ERP-взаимодействий с целью.
    /// Собственные действия персонажа эта настройка не ограничивает.
    /// </summary>
    private bool HasTargetErpLock(EntityUid user, EntityUid target)
    {
        if (user == target)
            return false;

        if (!TryComp<ErpStatusComponent>(target, out var status))
            return false;

        // При «Без ERP» запрет действует независимо от галочки.
        if (status.Erp == Erp.No)
            return true;

        // При «Полном ERP» дополнительный запрет не применяется.
        if (status.Erp != Erp.Ask)
            return false;

        // При «Неполном ERP» учитываем настройку цели.
        return TryComp<InteractionsComponent>(target, out var interactions)
            && interactions.LockErpInteractions;
    }

    /// <summary>
    /// Проверяет ERP-метку прототипа и действующий запрет цели.
    /// Используется при формировании списка и перед выполнением действия.
    /// </summary>
    private bool IsInteractionBlockedByErp(
        EntityUid user,
        EntityUid target,
        InteractionPrototype interaction)
    {
        return interaction.IsErp && HasTargetErpLock(user, target);
    }
}