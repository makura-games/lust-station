using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Lust.Preferences;
/// <summary>
/// Клас генератор рандомного текста для полей  портфолио
/// </summary>
public static class PortfolioGenerator
{
    // Шанс взять расовый вариант из пула вместо общего.
    private const float RacialVariantChance = 0.5f;

    /// <summary>
    /// Выбирает строку из пула. Пустая строка, если пула нет или он пуст.
    /// </summary>
    public static string Generate(string poolId, ProtoId<SpeciesPrototype> species)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var random = IoCManager.Resolve<IRobustRandom>();

        if (!protoMan.TryIndex<PortfolioPoolPrototype>(poolId, out var pool))
            return string.Empty;

        var strings = pool.General ?? new List<string>();
        if (pool.Species.TryGetValue(species.Id, out var racial)
            && racial is { Count: > 0 }
            && random.Prob(RacialVariantChance))
        {
            strings = racial;
        }

        if (strings.Count == 0)
            return string.Empty;

        return Loc.GetString(random.Pick(strings));
    }

    /// <summary>
    /// Случайное значение из энума.
    /// </summary>
    public static T PickEnum<T>() where T : struct, Enum
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        return random.Pick(Enum.GetValues<T>());
    }
}
