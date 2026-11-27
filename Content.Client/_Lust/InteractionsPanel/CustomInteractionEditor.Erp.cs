namespace Content.Client._Sunrise.InteractionsPanel;

    /// <summary>
    /// Устанавливает состояние галочки ERP по значению редактируемого действия.
    /// Вызывается при заполнении редактора данными.
    /// </summary>

public sealed partial class CustomInteractionEditor
{
    private void LoadErpSetting()
    {
        IsErpCheckBox.Pressed = _interaction.IsErp;
    }

    private void SaveErpSetting()
    {
        _interaction.IsErp = IsErpCheckBox.Pressed;
    }
}