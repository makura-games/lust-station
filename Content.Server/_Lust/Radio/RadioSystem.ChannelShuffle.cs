using Content.Server.StationEvents.Events;
using Content.Shared.Radio;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioSystem
{
    [Dependency]
    private RadioChannelShuffleRuleSystem _lustChannelShuffle = default!;

    // Возвращает false, если перенаправление было выбрано, но исходная передача запрещена или отсутствует связь.
    private bool TryApplyChannelShuffle(
        EntityUid radioSource,
        ref RadioChannelPrototype channel)
    {
        if (!_lustChannelShuffle.TryRedirect(
                radioSource,
                channel,
                out var redirectedChannel))
        {
            // Перенаправление не выпало: обычный код ниже сам проверит исходный канал.
            return true;
        }

        // Проверяем исходный канал перед заменой.
        var originalAttempt = new RadioSendAttemptEvent(channel, radioSource);

        RaiseLocalEvent(ref originalAttempt);
        RaiseLocalEvent(radioSource, ref originalAttempt);

        if (originalAttempt.Cancelled)
            return false;

        var sourceMapId = Transform(radioSource).MapID;

        // Проверяем связь исходного канала теми же условиями, которые используются штатной системой радио.
        var needsServer =
            !channel.LongRange &&
            !_exemptQuery.HasComp(radioSource);

        if (needsServer && !HasActiveServer(sourceMapId, channel.ID))
            return false;

        // Меняем канал до формирования подписи сообщения и выбора получателей.
        channel = redirectedChannel;

        // Новый канал затем проходит обычные проверки в существующем SendRadioMessage.
        return true;
    }
}