using Content.Shared._Lust.MedicalRecords;
using Robust.Client.UserInterface;

namespace Content.Client._Lust.MedicalRecords;

/// <summary>
/// Клиентский интерфейс медицинской консоли: открывает окно и гоняет сообщения с сервером.
/// </summary>
public sealed class LustMedicalRecordsBoundUserInterface : BoundUserInterface
{
    private LustMedicalRecordsWindow? _window;

    public LustMedicalRecordsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }


    /// <summary>
    /// Функция окрытия UI формирует сообщения для получешния данных об профилях
    /// </summary>
    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LustMedicalRecordsWindow>();

        _window.OnRecordSelected += selectedKey =>
            SendMessage(new LustMedicalRecordsSelectRecordMessage(selectedKey));

        _window.OnSaveRequested += (recordId, gender, species, physiologicalTraits, psychologicalTraits, notes, fingerprint, dna) =>
            SendMessage(new LustMedicalRecordsSaveMessage(
                recordId,
                gender,
                species,
                physiologicalTraits,
                psychologicalTraits,
                notes,
                fingerprint,
                dna));

        _window.OnPrintRequested += recordId =>
            SendMessage(new LustMedicalRecordsPrintMessage(recordId));
    }

    /// <summary>
    /// Функция обновления снимка карточки пользователя
    /// Вызывается сервером при открытии, смене выбора и любых изменениях листинга.
    /// </summary>
    /// <param name="state"></param>
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not LustMedicalRecordsUiState medicalState)
            return;

        _window?.UpdateState(medicalState);
    }

    /// <summary>
    ///  Принимает направленное сообщение с деталями выбранной записи.
    /// В отличие от <see cref="UpdateState"/>, приходит только тому актору,
    /// который прошёл проверку доступа, и содержит полный профиль.
    /// </summary>
    /// <param name="message"></param>
    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is LustMedicalRecordsRecordDetailsMessage details)
            _window?.UpdateRecordDetails(details);
    }
}
