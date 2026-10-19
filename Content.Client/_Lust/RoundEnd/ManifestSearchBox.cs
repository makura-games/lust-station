using System.Collections.Generic;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Lust.RoundEnd
{

    public sealed class ManifestSearchBox : BoxContainer
    {
        private readonly List<(Control Row, string SearchText)> _entries = new();
        private readonly LineEdit _search;

        public ManifestSearchBox()
        {
            Orientation = LayoutOrientation.Horizontal;
            Margin = new Thickness(0, 0, 0, 6);

            _search = new LineEdit
            {
                HorizontalExpand = true,
                PlaceHolder = Loc.GetString("lust-manifest-search-placeholder"),
            };
            _search.OnTextChanged += _ => ApplyFilter();

            AddChild(_search);
        }

        public void Register(Control row, string searchText)
        {
            _entries.Add((row, searchText.ToLowerInvariant()));


            var query = _search.Text.Trim().ToLowerInvariant();
            row.Visible = query.Length == 0 || ContainsQuery(searchText, query);
        }

        private void ApplyFilter()
        {
            var query = _search.Text.Trim().ToLowerInvariant();

            foreach (var (row, searchText) in _entries)
            {
                row.Visible = query.Length == 0 || ContainsQuery(searchText, query);
            }
        }

        private static bool ContainsQuery(string searchText, string query)
        {
            return searchText.ToLowerInvariant().Contains(query);
        }
    }
}
