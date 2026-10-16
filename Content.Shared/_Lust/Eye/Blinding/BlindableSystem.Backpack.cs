using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.Eye.Blinding.Systems;

/// <summary>
/// Система, вызывающая слепоту у любых существ со зрением (BlindableComponent),
/// когда они помещены внутрь сумки или иного предмета-хранилища с StorageComponent.
/// Не зависит от урона глаз и не лечится окулином, пока сущность остаётся внутри.
/// </summary>
public sealed partial class LustBlindableBackpackSystem : EntitySystem
{
    [Dependency] private BlindableSystem _blindable = new();
    [Dependency] private SharedPopupSystem _popup = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlindableComponent, EntGotInsertedIntoContainerMessage>(OnGotInserted);
        SubscribeLocalEvent<BlindableComponent, EntGotRemovedFromContainerMessage>(OnGotRemoved);
        SubscribeLocalEvent<BlindableComponent, CanSeeAttemptEvent>(OnCanSeeAttempt);
    }

    private void OnGotInserted(Entity<BlindableComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        // Проверяем, что контейнер принадлежит хранилищу предметов (сумки, рюкзаки, коробки)
        if (!HasComp<StorageComponent>(args.Container.Owner))
            return;

        ent.Comp.InStorage = true;
        Dirty(ent);

        _blindable.UpdateIsBlind(ent.Owner);

        // Персональное уведомление для игрока (поддержка клиента и сервера)
        _popup.PopupClient(Loc.GetString("blind-in-storage-blinded"), ent.Owner, ent.Owner);
    }

    private void OnGotRemoved(Entity<BlindableComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!ent.Comp.InStorage)
            return;

        ent.Comp.InStorage = false;
        Dirty(ent);

        _blindable.UpdateIsBlind(ent.Owner);
    }

    private static void OnCanSeeAttempt(Entity<BlindableComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (!ent.Comp.InStorage)
            return;

        // Блокируем зрение на уровне события
        args.Cancel();
    }
}
