namespace Content.Shared._Lust.Smell.Components;

/// <summary>
/// Маркер «может быть жертвой»: ставится на базового предка всех живых
/// существ (MobDamageable). Позволяет локальной системе запахов среагировать
/// на AttackedEvent по жертве, не занимая эксклюзивную пару событий.
/// Служит лишь ключом подписки — своих данных не несёт.
/// </summary>
[RegisterComponent]
public sealed partial class ScentOnAttackedComponent : Component
{
}
