namespace Content.Shared._Lust.Smell;

/// <summary>
/// Личный аромат персонажа: детерминированно сгенерированные цвет и ноты
/// из профиля расы и seed'а персонажа. Одинаковый seed — одинаковая сигнатура.
/// </summary>
public sealed record ScentSignature(Color Color, IReadOnlyList<LocId> Notes);
