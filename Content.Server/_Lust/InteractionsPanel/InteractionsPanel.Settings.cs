using Content.Shared._Lust.ErpStatus;
using Content.Shared._Lust.InteractionsPanel;
using Content.Shared._Sunrise.InteractionsPanel.Data.Components;
using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;

namespace Content.Server._Sunrise.InteractionsPanel;

public partial class InteractionsPanel
{
    private void OnSetErpInteractionLock(
        Entity<InteractionsComponent> ent,
        ref SetErpInteractionLockMessage args)
    {
        TrySetErpInteractionLock(ent, args.Actor, args.Locked);
    }

    /// <summary>
    /// Пытается изменить запрет ERP-взаимодействий персонажа.
    /// </summary>
    public bool TrySetErpInteractionLock(
        Entity<InteractionsComponent> ent,
        EntityUid actor,
        bool locked)
    {
        if (!CanSetErpInteractionLock(ent, actor, locked))
            return false;

        SetErpInteractionLock(ent, locked);
        return true;
    }

    /// <summary>
    /// Проверяет право изменить настройку и необходимость изменения.
    /// </summary>
    public bool CanSetErpInteractionLock(
        Entity<InteractionsComponent> ent,
        EntityUid actor,
        bool locked)
    {
        // Изменять настройку может только сам владелец персонажа.
        if (actor != ent.Owner)
            return false;

        // Настройка доступна только при «Неполном ERP».
        if (!TryComp<ErpStatusComponent>(ent.Owner, out var erpStatus)
            || erpStatus.Erp != Erp.Ask)
        {
            return false;
        }

        // Повторная установка того же значения не требует изменений.
        return ent.Comp.LockErpInteractions != locked;
    }

    /// <summary>
    /// Применяет проверенное значение и обновляет открытые панели.
    /// Вызывается только после успешной проверки CanSetErpInteractionLock.
    /// </summary>
    private void SetErpInteractionLock(
        Entity<InteractionsComponent> ent,
        bool locked)
    {
        ent.Comp.LockErpInteractions = locked;

        Dirty(ent.Owner, ent.Comp);
        UpdateUIForClothingChange(ent.Owner);
    }
}