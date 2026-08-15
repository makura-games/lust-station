namespace Content.Shared._Lust.Smell.Components;

/// <summary>
/// Маркер «может быть жертвой»: ставится на базового предка всех живых
/// существ (MobDamageable). Позволяет локальной системе запахов среагировать
/// на AttackedEvent по жертве, не занимая эксклюзивную пару
/// (MobStateComponent, AttackedEvent), которую может захотеть апстрим-контент.
/// Служит лишь ключом подписки — своих данных не несёт.
/// </summary>
[RegisterComponent]
public sealed partial class ScentOnAttackedComponent : Component
{
}
