using Content.Client.Gameplay;
using Content.Client.Ghost;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Client._Lust.DmArena; // Lust-edit
using Content.Shared.Ghost.Components;
using Content.Shared.Ghost.Systems;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Content.Client._Sunrise.ServersHub;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Configuration;
using Content.Shared._Sunrise.NewLife;
using GhostWarpsResponseEvent = Content.Shared.Ghost.Systems.SharedGhostSystem.GhostWarpsResponseEvent; // Sunrise-Edit - используем расширенный ответ панели призрака

namespace Content.Client.UserInterface.Systems.Ghost;

// TODO hud refactor BEFORE MERGE fix ghost gui being too far up
public sealed partial class GhostUIController : UIController, IOnSystemChanged<GhostSystem>, IOnSystemChanged<DmArenaGhostSystem> // Lust-edit
{
    [Dependency] private IEntityNetworkManager _net = default!;
    [Dependency] private ServersHubManager _serversHubManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    private ISharedSponsorsManager? _sponsorsManager; // Sunrise-Sponsors

    [UISystemDependency] private readonly GhostSystem? _system = default;
    [UISystemDependency] private readonly DmArenaGhostSystem? _dmArena = default; // Lust-edit

    private GhostGui? Gui => UIManager.GetActiveUIWidgetOrNull<GhostGui>();

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;

        IoCManager.Instance!.TryResolveType(out _sponsorsManager); // Sunrise-Sponsors
    }

    private void OnScreenLoad()
    {
        LoadGui();
    }

    private void OnScreenUnload()
    {
        UnloadGui();
    }

    public void OnSystemLoaded(GhostSystem system)
    {
        system.PlayerRemoved += OnPlayerRemoved;
        system.PlayerUpdated += OnPlayerUpdated;
        system.PlayerAttached += OnPlayerAttached;
        system.PlayerDetached += OnPlayerDetached;
        system.GhostWarpsResponse += OnWarpsResponse;
        system.GhostRoleCountUpdated += OnRoleCountUpdated;
    }

    public void OnSystemUnloaded(GhostSystem system)
    {
        system.PlayerRemoved -= OnPlayerRemoved;
        system.PlayerUpdated -= OnPlayerUpdated;
        system.PlayerAttached -= OnPlayerAttached;
        system.PlayerDetached -= OnPlayerDetached;
        system.GhostWarpsResponse -= OnWarpsResponse;
        system.GhostRoleCountUpdated -= OnRoleCountUpdated;
    }

    public void UpdateGui()
    {
        if (Gui == null)
        {
            return;
        }

        Gui.Visible = _system?.IsGhost ?? false;

        // Sunrise-Start
        var newLifeEnable = _cfg.GetCVar(SunriseCCVars.NewLifeEnable);
        var canRespawn = false;
        if (newLifeEnable)
        {
            var sponsorOnly = _cfg.GetCVar(SunriseCCVars.NewLifeSponsorOnly);
            if (sponsorOnly && _sponsorsManager != null)
            {
                if (_sponsorsManager.ClientAllowedRespawn() || !sponsorOnly)
                {
                    canRespawn = true;
                }
                else
                {
                    canRespawn = false;
                }
            }
            else
            {
                canRespawn = true;
            }
        }
        // Sunrise-End

        Gui.Update(_system?.AvailableGhostRoleCount, _system?.Player?.CanReturnToBody, canRespawn);
    }

    private void OnPlayerRemoved(GhostComponent component)
    {
        Gui?.Hide();
    }

    private void OnPlayerUpdated(GhostComponent component)
    {
        UpdateGui();
    }

    private void OnPlayerAttached(GhostComponent component)
    {
        if (Gui == null)
            return;

        Gui.Visible = true;
        UpdateGui();
    }

    private void OnPlayerDetached()
    {
        Gui?.Hide();
    }

    private void OnWarpsResponse(GhostWarpsResponseEvent msg)
    {
        if (Gui?.TargetWindow is not { } window)
            return;

        window.UpdateWarps(msg.Players, msg.Places, msg.Antagonists);
        window.Populate();
    }

    private void OnRoleCountUpdated(GhostUpdateGhostRoleCountEvent msg)
    {
        UpdateGui();
    }

    private void OnWarpClicked(NetEntity player)
    {
        var msg = new GhostWarpToTargetRequestEvent(player);
        _net.SendSystemNetworkMessage(msg);
    }

    private void OnGhostnadoClicked()
    {
        var msg = new GhostnadoRequestEvent();
        _net.SendSystemNetworkMessage(msg);
    }

    private void OnWarpToRandomFollowedClicked()
    {
        var msg = new WarpToRandomFollowedRequestEvent();
        _net.SendSystemNetworkMessage(msg);
    }

    private void OnWarpToRandomClicked()
    {
        var msg = new WarpToRandomRequestEvent();
        _net.SendSystemNetworkMessage(msg);
    }

    public void LoadGui()
    {
        if (Gui == null)
            return;

        Gui.RequestWarpsPressed += RequestWarps;
        Gui.ReturnToBodyPressed += ReturnToBody;
        Gui.GhostRolesPressed += GhostRolesPressed;
        Gui.RespawnPressed += Respawn; // Sunrise-Sponsors
        Gui.ChangeServerPressed += ChangeServerPressed;
        Gui.DmArenaPressed += DmArenaPressed; // Lust-edit
        Gui.TargetWindow.WarpClicked += OnWarpClicked;
        Gui.TargetWindow.OnGhostnadoClicked += OnGhostnadoClicked;
        Gui.TargetWindow.OnWarpToRandomFollowedClicked += OnWarpToRandomFollowedClicked;
        Gui.TargetWindow.OnWarpToRandomClicked += OnWarpToRandomClicked;

        UpdateGui();
        UpdateDmArena(); // Lust-edit
    }

    public void UnloadGui()
    {
        if (Gui == null)
            return;

        Gui.RequestWarpsPressed -= RequestWarps;
        Gui.ReturnToBodyPressed -= ReturnToBody;
        Gui.GhostRolesPressed -= GhostRolesPressed;
        Gui.RespawnPressed -= Respawn; // Sunrise-Sponsors
        Gui.ChangeServerPressed -= ChangeServerPressed;
        Gui.DmArenaPressed -= DmArenaPressed; // Lust-edit
        Gui.TargetWindow.WarpClicked -= OnWarpClicked;

        Gui.Hide();
    }

    // Lust-start
    public void OnSystemLoaded(DmArenaGhostSystem system)
    {
        system.StateChanged += UpdateDmArena;
        UpdateDmArena();
    }

    public void OnSystemUnloaded(DmArenaGhostSystem system)
    {
        system.StateChanged -= UpdateDmArena;
        Gui?.UpdateDmArena(0, false);
    }

    private void DmArenaPressed()
    {
        _dmArena?.RequestJoin();
    }

    private void UpdateDmArena()
    {
        if (_dmArena != null)
            Gui?.UpdateDmArena(_dmArena.PlayerCount, _dmArena.Enabled);
    }
    // Lust-end

    private void ReturnToBody()
    {
        _system?.ReturnToBody();
    }

    // Sunrise-Sponsors-Start
    private void Respawn()
    {
        var msg = new NewLifeOpenRequest();
        _net.SendSystemNetworkMessage(msg);
    }
    // Sunrise-Sponsors-End

    private void RequestWarps()
    {
        _system?.RequestWarps();
        Gui?.TargetWindow.Populate();
        Gui?.TargetWindow.OpenCentered();
    }

    private void GhostRolesPressed()
    {
        _system?.OpenGhostRoles();
    }

    private void ChangeServerPressed()
    {
        _serversHubManager.ToggleWindow();
    }
}
