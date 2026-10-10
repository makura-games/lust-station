using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server._Lust.Chat.Commands;

[AnyCommand]
internal sealed class AntiGhostCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override string Command => "antighost";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (player.Status != SessionStatus.InGame ||
            player.AttachedEntity is not { Valid: true } playerEntity ||
            args.Length < 1)
        {
            return;
        }

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        _chat.TrySendAntiGhostMessage(playerEntity, message, shell, player);
    }
}
