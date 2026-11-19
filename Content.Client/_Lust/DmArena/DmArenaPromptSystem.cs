using Content.Shared._Lust.DmArena;

namespace Content.Client._Lust.DmArena;

public sealed class DmArenaPromptSystem : EntitySystem
{
    private DmArenaReturnWindow? _revivalWindow;
    private DmArenaLeaveWindow? _exitWindow;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<DmArenaRevivalOfferEvent>(OnRevivalOffer);
        SubscribeNetworkEvent<DmArenaExitOfferEvent>(OnExitOffer);
    }

    private void OnRevivalOffer(DmArenaRevivalOfferEvent ev)
    {
        _revivalWindow?.Close();
        _revivalWindow = new DmArenaReturnWindow();
        _revivalWindow.OnAccepted += () => RaiseNetworkEvent(new DmArenaRevivalAcceptEvent());
        _revivalWindow.OpenCentered();
    }

    private void OnExitOffer(DmArenaExitOfferEvent ev)
    {
        _exitWindow?.Close();
        _exitWindow = new DmArenaLeaveWindow();
        _exitWindow.OnChoice += stay => RaiseNetworkEvent(new DmArenaExitChoiceEvent(stay));
        _exitWindow.OpenCentered();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _revivalWindow?.Close();
        _revivalWindow = null;
        _exitWindow?.Close();
        _exitWindow = null;
    }
}
