using Content.Shared._Sunrise.CopyMachine;
using Content.Shared.Examine;

namespace Content.Shared._Lust.CopyMachine;

/// <summary>
/// Строка про заклинивание в осмотре принтера.(Я сделал отдельную систему вместо
/// partial от SharedCopyMachineSystem.cs так как он не используется ни где на сервере у санрайза)
/// </summary>
public sealed partial class CopyMachineExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CopyMachineComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    /// Добавляет строку про заклинивание в осмотр принтера.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnExamined(Entity<CopyMachineComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.IsJammed)
            return;
        args.PushMarkup(Loc.GetString("copy-machine-lust-examined-jammed"));
    }
}
