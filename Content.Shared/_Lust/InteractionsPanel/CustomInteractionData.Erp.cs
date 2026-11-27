namespace Content.Shared._Sunrise.InteractionsPanel.Data.UI;

public sealed partial class CustomInteractionData
{
    /// <summary>
    /// Указывает, что автор пометил кастомное действие как ERP.
    /// Передаётся клиентом вместе с данными выполняемого действия.
    /// По умолчанию true, как и в клиентской модели.
    /// </summary>
    public bool IsErp { get; set; } = true;
}