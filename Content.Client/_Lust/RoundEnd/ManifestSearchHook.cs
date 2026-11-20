using System.Collections.Generic;
using Content.Shared.GameTicking;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Lust.RoundEnd
{

    public static class ManifestSearchHook
    {

        public static void Apply(
            BoxContainer tab,
            IReadOnlyList<Control> rows,
            IReadOnlyList<RoundEndMessageEvent.RoundEndPlayerInfo> players)
        {
            var searchBox = new ManifestSearchBox();
            tab.AddChild(searchBox);
            searchBox.SetPositionFirst();

            var count = rows.Count < players.Count ? rows.Count : players.Count;
            for (var i = 0; i < count; i++)
            {
                var role = string.IsNullOrEmpty(players[i].Role) ? string.Empty : Loc.GetString(players[i].Role);
                var searchText = $"{players[i].PlayerOOCName} {players[i].PlayerICName} {role}";
                searchBox.Register(rows[i], searchText);
            }
        }
    }
}
