using System.Linq;
using Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;
using Content.Shared.Humanoid;

namespace Content.Client._Sunrise.InteractionsPanel;

public sealed partial class InteractionsUIWindow
{
    private partial void UpdateLustEntityInformation(bool selfTargeting, Sex? userSex, Sex? targetSex)
    {
        if (userSex is { } currentUserSex)
        {
            var userGenitals = GenitalsHelper.GetGenitals(currentUserSex)
                .OrderBy(slot => GenitalDisplayOrder.GetValueOrDefault(slot, 99))
                .ToList();

            var userGenitalsText = string.Join(", ", userGenitals.Select(GenitalSlotToString));
            SetGenitalsText(UserGenitalsLabel, userGenitalsText);
        }
        else
        {
            SetGenitalsText(UserGenitalsLabel, "???");
        }

        if (!selfTargeting && targetSex is { } currentTargetSex)
        {
            var targetGenitals = GenitalsHelper.GetGenitals(currentTargetSex)
                .OrderBy(slot => GenitalDisplayOrder.GetValueOrDefault(slot, 99))
                .ToList();
            var targetGenitalsText = string.Join(", ", targetGenitals.Select(GenitalSlotToString));
            SetGenitalsText(TargetGenitalsLabel, targetGenitalsText);
        }
        else
        {
            SetGenitalsText(TargetGenitalsLabel, "");
        }
    }
}
