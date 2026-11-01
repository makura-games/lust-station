using Content.Shared._Lust.ErpStatus;
using Content.Shared._Lust.InteractionsPanel;
using Content.Shared._Sunrise.InteractionsPanel.Data.Components;
using Robust.Shared.GameObjects;

namespace Content.Server._Sunrise.InteractionsPanel;

public partial class InteractionsPanel
{
    private void OnSetErpInteractionLock(
        Entity<InteractionsComponent> ent,
        ref SetErpInteractionLockMessage args)
    {
        if (args.Actor != ent.Owner)
            return;

        // Настройка доступна только при статусе «Неполное ERP».
        if (!TryComp<ErpStatusComponent>(ent.Owner, out var erpStatus)
            || erpStatus.Erp != Content.Shared.Humanoid.Erp.Ask)
        {
            return;
        }

        if (ent.Comp.LockErpInteractions == args.Locked)
            return;

        ent.Comp.LockErpInteractions = args.Locked;

        Dirty(ent.Owner, ent.Comp);
    }
}