using Content.Shared._Sunrise.CopyMachine;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;

#pragma warning disable IDE0130
namespace Content.Server._Sunrise.CopyMachine;

public sealed partial class CopyMachineSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    /// <summary>
    /// Функция вызываемая ударом по принтеру, для попытки починки принтера
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnAttacked(Entity<CopyMachineComponent> ent, ref AttackedEvent args)
    {
        if (!ent.Comp.IsJammed)
            return;

        if (!_random.Prob(ent.Comp.UnjamChance))
            return;

        ent.Comp.IsJammed = false;
        Dirty(ent);
        UpdateJamAppearance(ent, false);
        _popup.PopupEntity(Loc.GetString("copy-machine-lust-unjammed"), ent, args.User, PopupType.Medium);
        _audioSystem.PlayPvs(ent.Comp.UnjamSound, ent);

    }

    /// <summary>
    /// Функция при заклиненом принтере вызывает звук заклина, при целом пробует заклинить принтер с шансом
    /// </summary>
    /// <param name="ent"></param>
    /// <returns></returns>
    private bool TryStartJamJob(Entity<CopyMachineComponent> ent)
    {
        if (ent.Comp.IsJammed)
        {
            ent.Comp.CurrentJobWillJam = true;
            _audioSystem.PlayPvs(ent.Comp.JamSound, ent);
            return true;
        }

        if (_random.Prob(ent.Comp.JamChance))
        {
            ent.Comp.IsJammed = true;
            ent.Comp.CurrentJobWillJam = true;
            Dirty(ent);
            UpdateJamAppearance(ent, true);
            _popup.PopupEntity(Loc.GetString("copy-machine-lust-jammed"), ent, PopupType.Medium);
            _audioSystem.PlayPvs(ent.Comp.JamSound, ent);
            return true;
        }

        ent.Comp.CurrentJobWillJam = false;
        return false;
    }

    /// <summary>
    /// Функция спавнит макулатуру если принтер заклинил
    /// </summary>
    /// <param name="ent"></param>
    /// <returns></returns>
    private bool TryCompleteJamJob(Entity<CopyMachineComponent> ent)
    {
        if (!ent.Comp.CurrentJobWillJam)
            return false;

        ent.Comp.CurrentJobWillJam = false;
        var scrap = SpawnAtPosition(ent.Comp.JammedPaperProtoId, Transform(ent).Coordinates);

        if (!ent.Comp.IsJammed)
        {
            var dir = _random.NextAngle().ToVec() * ent.Comp.JamPaperDropDistance;
            _throwing.TryThrow(scrap, dir, ent.Comp.JamPaperDropSpeed);
        }

        return true;
    }

    /// <summary>
    /// Функция меняет спрайт принтера заклиненый/не заклиненый
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="isJammed"></param>
    private void UpdateJamAppearance(EntityUid uid, bool isJammed)
    {
        _appearance.SetData(uid, CopyMachineVisuals.IsJammed, isJammed);
    }
}
