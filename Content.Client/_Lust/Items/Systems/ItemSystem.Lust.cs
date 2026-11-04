using Content.Shared.Item;
using Robust.Client.Graphics;

namespace Content.Client.Items.Systems;

public sealed partial class ItemSystem
{
    private static bool TryGetLustDefaultHeldState(RSI rsi, ItemComponent item, string defaultKey, out string state)
    {
        if (item.HeldPrefix != null && rsi.TryGetState(defaultKey, out _))
        {
            state = defaultKey;
            return true;
        }

        state = defaultKey;
        return false;
    }
}
