namespace Content.Shared._Sunrise.InteractionsPanel.Data.UI;

public sealed partial class InteractionWindowBoundUserInterfaceState
{
    /// <summary>
    /// Действует ли ERP-запрет цели для пользователя этой панели.
    /// Нужен для фильтрации клиентских кастомных действий.
    /// </summary>
    public bool TargetErpLocked { get; set; }
}