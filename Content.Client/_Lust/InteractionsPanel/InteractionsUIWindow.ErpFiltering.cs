namespace Content.Client._Sunrise.InteractionsPanel;

public sealed partial class InteractionsUIWindow
{
    private bool _targetErpLocked;

    /// <summary>
    /// Принимает серверный запрет цели перед обновлением списка кнопок.
    /// Не изменяет и не удаляет сохранённые кастомные действия.
    /// </summary>
    public void SetTargetErpLock(bool locked)
    {
        _targetErpLocked = locked;
    }
}