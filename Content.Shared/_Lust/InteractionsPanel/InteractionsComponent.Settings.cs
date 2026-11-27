namespace Content.Shared._Sunrise.InteractionsPanel.Data.Components;

    /// <summary>
    /// Запрет ERP-взаимодействий других игроков с этим персонажем.
    /// Не ограничивает обычные взаимодействия через панель.
    /// Изменение настройки доступно только при статусе «Неполное ERP».
    /// Не сохраняется в профиль: в новом раунде значение снова false.
    /// </summary>
public sealed partial class InteractionsComponent
{
    [ViewVariables, AutoNetworkedField]
    public bool LockErpInteractions = false;
}