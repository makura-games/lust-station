using System.Linq;
using Content.Shared._Lust.Borgs;
using Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;
using Content.Shared.Humanoid;
using Robust.Shared.Network;

namespace Content.Client._Sunrise.InteractionsPanel;

public sealed partial class InteractionsUIWindow
{
    private partial void UpdateLustEntityInformation(NetEntity userEntity, NetEntity targetEntity, bool selfTargeting)
    {
        if (BorgSexSystem.TryGetSex(_entityManager.GetEntity(userEntity), _entityManager, out var userSex))
        {
            var userGenitals = GenitalsHelper.GetGenitals(userSex)
                .OrderBy(slot => GenitalDisplayOrder.GetValueOrDefault(slot, 99))
                .ToList();

            var userGenitalsText = string.Join(", ", userGenitals.Select(GenitalSlotToString));
            SetGenitalsText(UserGenitalsLabel, userGenitalsText);
        }
        else
        {
            SetGenitalsText(UserGenitalsLabel, "???");
        }

        if (!selfTargeting &&
            BorgSexSystem.TryGetSex(_entityManager.GetEntity(targetEntity), _entityManager, out var targetSex))
        {
            var targetGenitals = GenitalsHelper.GetGenitals(targetSex)
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
