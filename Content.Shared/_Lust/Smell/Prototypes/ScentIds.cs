using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell.Prototypes;

/// <summary>
/// Единый источник id запахов, используемых серверной логикой системы запахов
/// (ссылки на прототипы из scents.yml). Константы для предметных эмиторов
/// и статус-запахов не дублируются — там id задаётся прямо в YAML предмета.
/// </summary>
public static class ScentIds
{
    [ValidatePrototypeId<ScentPrototype>]
    public const string Arousal = "LustArousal";

    [ValidatePrototypeId<ScentPrototype>]
    public const string Orgasm = "LustOrgasm";

    [ValidatePrototypeId<ScentPrototype>]
    public const string Blood = "LustBlood";

    [ValidatePrototypeId<ScentPrototype>]
    public const string OtherBlood = "LustOtherBlood";

    [ValidatePrototypeId<ScentPrototype>]
    public const string Bruise = "LustBruise";

    [ValidatePrototypeId<ScentPrototype>]
    public const string Poison = "LustPoison";
}