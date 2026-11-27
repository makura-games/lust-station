using System.IO;

namespace Content.Client._Sunrise.InteractionsPanel.Models;

public sealed partial class CustomInteractionService
{
    private void LoadErpSetting(
        CustomInteraction interaction,
        string value)
    {
        // Некорректное значение трактуем как ERP.
        // При отсутствии строки остаётся начальное IsErp = true.
        interaction.IsErp =
            !bool.TryParse(value, out var isErp) || isErp;
    }

    private void SaveErpSetting(
        StreamWriter writer,
        CustomInteraction interaction)
    {
        // Признак записывается в ту же запись, что имя и сообщения действия.
        writer.WriteLine($"IS_ERP:{interaction.IsErp}");
    }
}