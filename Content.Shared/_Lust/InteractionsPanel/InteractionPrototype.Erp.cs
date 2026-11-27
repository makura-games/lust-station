namespace Content.Shared._Sunrise.InteractionsPanel.Data.Prototypes;

public sealed partial class InteractionPrototype
{
    /// <summary>
    /// Относится ли действие к ERP.
    /// При действующем ERP-запрете цели действие скрывается
    /// из списка и не может быть выполнено на сервере.
    /// Задаётся в YAML через isErp.
    /// </summary>
    [DataField("isErp")]
    public bool IsErp { get; private set; } = false;
}