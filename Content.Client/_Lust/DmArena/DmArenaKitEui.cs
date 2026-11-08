using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared._Lust.DmArena;
using JetBrains.Annotations;

namespace Content.Client._Lust.DmArena;

[UsedImplicitly]
public sealed class DmArenaKitEui : BaseEui
{
    private readonly DmArenaKitWindow _window;

    public DmArenaKitEui()
    {
        _window = new DmArenaKitWindow();
        _window.OnClose += OnWindowClosed;
        _window.OnLoadoutConfirmed += items => SendMessage(new DmArenaKitConfirmMessage(items));
        _window.OnPresetSaved += items => SendMessage(new DmArenaKitSaveMessage(items));
    }

    private void OnWindowClosed()
    {
        SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _window.OnClose -= OnWindowClosed;
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not DmArenaKitEuiState loadoutState)
            return;

        _window.UpdateState(loadoutState);
    }
}
