using Content.Shared.Access.Systems;
using Content.Shared._Lust.MedicalRecords;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Player;

namespace Content.Client._Lust.MedicalRecords;

/// <summary>
/// Клиентский интерфейс медицинской консоли
/// </summary>
public sealed class LustMedicalRecordsBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private LustMedicalRecordsWindow? _window;

    /// <summary>
    /// Функция окрытия UI формирует сообщения для получешния данных об профилях
    /// </summary>
    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LustMedicalRecordsWindow>();

        var playerManager = IoCManager.Resolve<IPlayerManager>();
        var accessReader = EntMan.System<AccessReaderSystem>();
        _window.CanSave = () =>
            playerManager.LocalEntity is { } local && accessReader.IsAllowed(local, Owner);

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
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not LustMedicalRecordsUiState medicalState)
            return;

        _window?.UpdateState(medicalState);
    }
}
