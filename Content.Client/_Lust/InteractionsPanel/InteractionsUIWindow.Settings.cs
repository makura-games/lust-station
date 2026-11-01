using Content.Shared._Lust.ErpStatus;
using Content.Shared._Lust.InteractionsPanel;
using Content.Shared._Sunrise.InteractionsPanel.Data.Components;
using Content.Shared.Humanoid;

namespace Content.Client._Sunrise.InteractionsPanel;

public sealed partial class InteractionsUIWindow
{
    private bool? _lastServerErpLock;

 private void UpdateErpLockSetting()
    {
        // Настройка принадлежит владельцу панели, а не выбранной цели.
        if (_owner == null
            || !_entityManager.TryGetComponent<InteractionsComponent>(
                _owner.Owner, out var interactions)
            || !_entityManager.TryGetComponent<ErpStatusComponent>(
                _owner.Owner, out var erpStatus))
        {
            ErpInteractionLockSettingsContainer.Visible = false;
            LockErpInteractionsCheckBox.Disabled = true;
            _lastServerErpLock = null;
            return;
        }

        // Для «Полного ERP» и «Без ERP» настройку вообще не показываем.
        var canChange = erpStatus.Erp == Erp.Ask;
        ErpInteractionLockSettingsContainer.Visible = canChange;
        LockErpInteractionsCheckBox.Disabled = !canChange;

        if (!canChange)
        {
            // При возвращении к «Неполному ERP» заново прочитаем
            // актуальное значение из сетевого компонента.
            _lastServerErpLock = null;
            return;
        }

        if (_lastServerErpLock != interactions.LockErpInteractions)
        {
            LockErpInteractionsCheckBox.Pressed =
                interactions.LockErpInteractions;
        }

        _lastServerErpLock = interactions.LockErpInteractions;
    }

    private void ApplyErpLockSetting()
    {
        if (_owner == null
            || !_entityManager.TryGetComponent<ErpStatusComponent>(
                _owner.Owner, out var erpStatus)
            || erpStatus.Erp != Erp.Ask
            || LockErpInteractionsCheckBox.Disabled)
        {
            return;
        }

        _owner.SendBoundUserInterfaceMessage(
            new SetErpInteractionLockMessage(
                LockErpInteractionsCheckBox.Pressed));
    }
}