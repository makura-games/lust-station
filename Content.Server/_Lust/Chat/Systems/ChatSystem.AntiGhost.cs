using Content.Server.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Players;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

#pragma warning disable IDE0130 // Пространство имён расширяемой системы не совпадает с папкой _Lust
namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private static readonly Color AntiGhostColor = Color.FromHex("#F4C1C1");

    /// <summary>
    /// Проверяет, может ли игрок отправить сообщение в антигост-чат.
    /// </summary>
    /// <param name="quiet">Не выводить игроку причину отказа.</param>
    public bool CanSendAntiGhostMessage(
        EntityUid source,
        string message,
        IConsoleShell shell,
        ICommonSession player,
        bool quiet = false)
    {
        if (HasComp<GhostComponent>(source) ||
            player.AttachedEntity != source)
        {
            return false;
        }

        if (!quiet)
            return CanSendInGame(message, shell, player);

        return player.ContentData()?.Mind != null &&
               player.AttachedEntity is { Valid: true } &&
               message.Length <= _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);
    }

    /// <summary>
    /// Пытается отправить сообщение в антигост-чат.
    /// </summary>
    /// <returns>Возвращает <see langword="true"/>, если сообщение было отправлено.</returns>
    public bool TrySendAntiGhostMessage(
        EntityUid source,
        string message,
        IConsoleShell shell,
        ICommonSession player)
    {
        if (!CanSendAntiGhostMessage(source, message, shell, player))
            return false;

        // HandleRateLimit изменяет состояние лимитера, поэтому эта проверка не входит в Can-метод.
        if (_chatManager.HandleRateLimit(player) != RateLimitStatus.Allowed)
            return false;

        if (!TryProcessSunriseChatMessage(source, ref message, oocChatType: InGameOOCChatType.Looc))
            return false;

        message = SanitizeInGameOOCMessage(message);
        if (string.IsNullOrEmpty(message))
            return false;

        SendAntiGhostMessage(source, message, player);
        return true;
    }

    private void SendAntiGhostMessage(EntityUid source, string message, ICommonSession player)
    {
        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));
        var localizedMessage = Loc.GetString("chat-manager-entity-antighost-wrap-message",
            ("entityName", name),
            ("message", message));
        var wrappedMessage = $"[color={AntiGhostColor.ToHex()}][italic]{localizedMessage}[/italic][/color]";
        var recipients = new List<INetChannel>();

        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is not { Valid: true } target)
                continue;

            if (TryComp<GhostComponent>(target, out var ghost))
            {
                if (ghost.CanGhostInteract)
                    recipients.Add(session.Channel);

                continue;
            }

            if (_examineSystem.InRangeUnOccluded(source, target, VoiceRange))
                recipients.Add(session.Channel);
        }

        _chatManager.ChatMessageToMany(
            ChatChannel.Emotes,
            message,
            wrappedMessage,
            source,
            false,
            false,
            recipients,
            AntiGhostColor,
            author: player.UserId);

        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"Anti-ghost chat from {ToPrettyString(source):Player}: {message}");
    }
}
