namespace Content.Client._Sunrise.InteractionsPanel.Models;

public sealed partial class CustomInteraction
{
    // Авторская отметка: действие содержит ERP.
    // Для старых записей без этого поля оставляем true:
    // они не должны автоматически считаться обычными действиями.
    public bool IsErp { get; set; } = true;
}