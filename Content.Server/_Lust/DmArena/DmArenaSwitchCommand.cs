using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared._Lust.DmArena;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Server._Lust.DmArena;

/// <summary>
/// Включает и выключает ДМ арену. Состояние хранится только на сервере, клиент его изменить не может.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed partial class DmArenaSwitchCommand : LocalizedCommands
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IEntitySystemManager _systems = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override string Command => "dmarena";
    public override string Description => LocalizationManager.GetString("cmd-dmarena-desc");
    public override string Help => LocalizationManager.GetString("cmd-dmarena-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is { } player && !_admin.HasAdminFlag(player, AdminFlags.Fun))
        {
            shell.WriteError(LocalizationManager.GetString("cmd-dmarena-no-access"));
            return;
        }

        if (args.Length > 0 && args[0].Equals("debug", StringComparison.OrdinalIgnoreCase))
        {
            ExecuteDebug(shell, args[1..]);
            return;
        }

        if (args.Length > 1)
        {
            shell.WriteError(LocalizationManager.GetString("shell-wrong-arguments-number"));
            return;
        }

        var enabled = _cfg.GetCVar(DmArenaConfigVars.DmArenaEnabled);
        var action = args.Length == 0 ? "status" : args[0].ToLowerInvariant();

        switch (action)
        {
            case "status":
                WriteStatus(shell, enabled);
                break;
            case "on" or "off":
                SetEnabled(shell, enabled, action == "on");
                break;
            default:
                shell.WriteError(LocalizationManager.GetString("cmd-dmarena-bad-argument"));
                break;
        }
    }

    private void WriteStatus(IConsoleShell shell, bool enabled)
    {
        shell.WriteLine(LocalizationManager.GetString(enabled ? "cmd-dmarena-status-on" : "cmd-dmarena-status-off"));
    }

    private void SetEnabled(IConsoleShell shell, bool current, bool value)
    {
        var who = shell.Player?.Name ?? "server";

        if (value)
            _systems.GetEntitySystem<DmArenaMatchSystem>().OpenManually();

        if (value == current)
        {
            if (value)
                _adminLog.Add(LogType.AdminCommands, LogImpact.High, $"{who} opened the DM arena manually");

            WriteStatus(shell, current);
            return;
        }

        _cfg.SetCVar(DmArenaConfigVars.DmArenaEnabled, value);

        _adminLog.Add(LogType.AdminCommands, LogImpact.High, $"{who} {(value ? "enabled" : "disabled")} the DM arena");

        shell.WriteLine(LocalizationManager.GetString(value ? "cmd-dmarena-enabled" : "cmd-dmarena-disabled"));
    }

    private void ExecuteDebug(IConsoleShell shell, string[] args)
    {
        var arena = _systems.GetEntitySystem<DmArenaMatchSystem>();
        var sub = args.Length == 0 ? "status" : args[0].ToLowerInvariant();

        switch (sub)
        {
            case "status":
                WriteFlag(shell, "combatlock", arena.DebugCombatLock);
                WriteFlag(shell, "refill", _cfg.GetCVar(DmArenaConfigVars.DmArenaRefill));
                shell.WriteLine(LocalizationManager.GetString("cmd-dmarena-debug-disabled-items", ("items", string.Join(", ", arena.DebugDisabledItems))));
                shell.WriteLine(LocalizationManager.GetString("cmd-dmarena-debug-no-cooldown", ("players", string.Join(", ", arena.DebugNoCooldown.Select(PlayerName)))));
                shell.WriteLine(LocalizationManager.GetString("cmd-dmarena-debug-rofls-players", ("players", string.Join(", ", arena.DebugRofls.Select(PlayerName)))));
                return;
            case "reset":
                arena.ResetDebug();
                arena.RefreshLoadouts();
                LogDebug(shell, "reset");
                shell.WriteLine(LocalizationManager.GetString("cmd-dmarena-debug-reset"));
                return;
        }

        if (!TryParseFlag(args, out var value))
        {
            shell.WriteError(LocalizationManager.GetString("cmd-dmarena-debug-bad-argument"));
            return;
        }

        switch (sub)
        {
            case "combatlock":
                arena.DebugCombatLock = value;
                break;
            case "refill":
                _cfg.SetCVar(DmArenaConfigVars.DmArenaRefill, value);
                break;
            case "item":
                if (!arena.ShopGearIds.Contains(args[1]))
                {
                    shell.WriteError(LocalizationManager.GetString("cmd-dmarena-debug-unknown-item"));
                    return;
                }

                if (value)
                    arena.DebugDisabledItems.Remove(args[1]);
                else
                    arena.DebugDisabledItems.Add(args[1]);

                arena.RefreshLoadouts();
                break;
            case "cooldown" or "rofls":
                if (!_player.TryGetSessionByUsername(args[1], out var session))
                {
                    shell.WriteError(LocalizationManager.GetString("cmd-dmarena-debug-unknown-player"));
                    return;
                }

                var set = sub == "rofls" ? arena.DebugRofls : arena.DebugNoCooldown;
                if (value == (sub == "rofls"))
                    set.Add(session.UserId);
                else
                    set.Remove(session.UserId);

                break;
            default:
                shell.WriteError(LocalizationManager.GetString("cmd-dmarena-debug-bad-argument"));
                return;
        }

        LogDebug(shell, $"{sub} {string.Join(' ', args.Skip(1))}");
        WriteFlag(shell, sub, value);
    }

    private static bool TryParseFlag(string[] args, out bool value)
    {
        value = false;
        var length = args[0].ToLowerInvariant() is "item" or "cooldown" or "rofls" ? 3 : 2;
        if (args.Length != length)
            return false;

        switch (args[^1].ToLowerInvariant())
        {
            case "true":
                value = true;
                return true;
            case "false":
                return true;
            default:
                return false;
        }
    }

    private void WriteFlag(IConsoleShell shell, string name, bool value)
    {
        shell.WriteLine(LocalizationManager.GetString("cmd-dmarena-debug-flag", ("name", name), ("value", value.ToString().ToLowerInvariant())));
    }

    private void LogDebug(IConsoleShell shell, string action)
    {
        var who = shell.Player?.Name ?? "server";
        _adminLog.Add(LogType.AdminCommands, LogImpact.High, $"{who} used DM arena debug: {action}");
    }

    private string PlayerName(NetUserId id)
    {
        return _player.TryGetSessionById(id, out var session) ? session.Name : id.ToString();
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                new[] { "on", "off", "status", "debug" },
                LocalizationManager.GetString("cmd-dmarena-hint"));
        }

        if (!args[0].Equals("debug", StringComparison.OrdinalIgnoreCase))
            return CompletionResult.Empty;

        var arena = _systems.GetEntitySystem<DmArenaMatchSystem>();

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(new[]
            {
                Flag("combatlock", arena.DebugCombatLock),
                Flag("refill", _cfg.GetCVar(DmArenaConfigVars.DmArenaRefill)),
                new CompletionOption("item"),
                new CompletionOption("cooldown"),
                new CompletionOption("rofls"),
                new CompletionOption("status"),
                new CompletionOption("reset"),
            }, LocalizationManager.GetString("cmd-dmarena-debug-hint"));
        }

        var sub = args[1].ToLowerInvariant();

        if (args.Length == 3 && sub == "item")
        {
            return CompletionResult.FromHintOptions(
                arena.ShopGearIds.Select(id => Flag(id, !arena.DebugDisabledItems.Contains(id))),
                LocalizationManager.GetString("cmd-dmarena-debug-item-hint"));
        }

        if (args.Length == 3 && sub == "cooldown")
        {
            return CompletionResult.FromHintOptions(
                _player.Sessions.Select(x => Flag(x.Name, !arena.DebugNoCooldown.Contains(x.UserId))),
                LocalizationManager.GetString("cmd-dmarena-debug-player-hint"));
        }

        if (args.Length == 3 && sub == "rofls")
        {
            return CompletionResult.FromHintOptions(
                _player.Sessions.Select(x => Flag(x.Name, arena.DebugRofls.Contains(x.UserId))),
                LocalizationManager.GetString("cmd-dmarena-debug-rofls-hint"));
        }

        var valueIndex = sub is "item" or "cooldown" or "rofls" ? 4 : 3;
        if (args.Length == valueIndex && sub is "combatlock" or "refill" or "item" or "cooldown" or "rofls")
        {
            return CompletionResult.FromHintOptions(
                new[] { new CompletionOption("true"), new CompletionOption("false") },
                LocalizationManager.GetString("cmd-dmarena-debug-value-hint"));
        }

        return CompletionResult.Empty;
    }

    private static CompletionOption Flag(string name, bool value)
    {
        return new CompletionOption(name, value.ToString().ToLowerInvariant());
    }
}
