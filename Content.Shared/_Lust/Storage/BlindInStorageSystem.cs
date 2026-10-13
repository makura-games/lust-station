using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Shared._Lust.Storage;

/// <summary>
/// Система, вызывающая слепоту у переносимых рас (фелинидов, резоми и т.д.),
/// когда они помещены внутрь сумки или иного предмета-хранилища с StorageComponent.
/// Не зависит от урона глаз и не лечится окулином, пока сущность остаётся внутри.
/// </summary>
public sealed partial class BlindInStorageSystem : EntitySystem
{
    [Dependency] private BlindableSystem _blindable = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlindInStorageComponent, EntGotInsertedIntoContainerMessage>(OnGotInserted);
        SubscribeLocalEvent<BlindInStorageComponent, EntGotRemovedFromContainerMessage>(OnGotRemoved);
        SubscribeLocalEvent<BlindInStorageComponent, CanSeeAttemptEvent>(OnCanSeeAttempt);
    }

    private void OnGotInserted(Entity<BlindInStorageComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        // Проверяем, что контейнер принадлежит хранилищу предметов (сумки, рюкзаки, коробки)
        if (!HasComp<StorageComponent>(args.Container.Owner))
            return;

        ent.Comp.InStorage = true;
        Dirty(ent);

        _blindable.UpdateIsBlind(ent.Owner);

        // Персональное уведомление для игрока
        _popup.PopupClient(Loc.GetString("blind-in-storage-blinded"), ent.Owner, ent.Owner);
    }

    private void OnGotRemoved(Entity<BlindInStorageComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!ent.Comp.InStorage)
            return;

        ent.Comp.InStorage = false;
        Dirty(ent);

        _blindable.UpdateIsBlind(ent.Owner);
    }

    private void OnCanSeeAttempt(Entity<BlindInStorageComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (!ent.Comp.InStorage)
            return;

        if (!HasComp<BlindableComponent>(ent))
            return;

        // Блокируем зрение на уровне события
        args.Cancel();
    }
}
