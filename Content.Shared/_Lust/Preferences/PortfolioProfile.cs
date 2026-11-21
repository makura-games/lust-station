using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Preferences;

/// <summary>
/// Класс портфолио хранит в себе всю информацию портфоли и медтоды, создания, капирования, сравнения, хеширования
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class PortfolioProfile : IEquatable<PortfolioProfile>
{
    // Лимиты длин. Должны совпадать с MaxLength на полях ProfilePortfolio в БД (Model.Lust.cs).
    private const int MaxTextLength = 512;
    private const int MaxShortTextLength = 128;

    /// <summary>
    /// Отличительные признаки (многострочное).
    /// </summary>
    [DataField]
    public string DistinguishingFeatures { get; set; } = string.Empty;

    /// <summary>
    /// Регион (однострочное).
    /// </summary>
    [DataField]
    public string Region { get; set; }  = string.Empty;

    /// <summary>
    /// Планета или колония (однострочное).
    /// </summary>
    [DataField]
    public string PlanetOrColony { get; set; } = string.Empty;

    /// <summary>
    /// Адрес (однострочное).
    /// </summary>
    [DataField]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Образование (многострочное).
    /// </summary>
    [DataField]
    public string Education { get; set; } = string.Empty;

    /// <summary>
    /// Трудовой опыт (многострочное).
    /// </summary>
    [DataField]
    public string WorkExperience { get; set; } = string.Empty;

    /// <summary>
    /// Семейное положение (enum).
    /// </summary>
    [DataField]
    public MaritalStatus MaritalStatusValue { get; set; } = MaritalStatus.Single;

    /// <summary>
    /// Близкие родственники (многострочное).
    /// </summary>
    [DataField]
    public string CloseRelatives { get; set; } = string.Empty;

    /// <summary>
    /// Экстренный контакт (многострочное).
    /// </summary>
    [DataField]
    public string EmergencyContact { get; set; } = string.Empty;

    /// <summary>
    /// Физиологические черты (многострочное).
    /// </summary>
    [DataField]
    public string PhysiologicalTraits { get; set; } = string.Empty;

    /// <summary>
    /// Психологические черты (многострочное).
    /// </summary>
    [DataField]
    public string PsychologicalTraits { get; set; } = string.Empty;

    /// <summary>
    /// История арестов (многострочное).
    /// </summary>
    [DataField]
    public string ArrestHistory { get; set; } = string.Empty;

    /// <summary>
    /// История судимостей (многострочное).
    /// </summary>
    [DataField]
    public string ConvictionHistory { get; set; } = string.Empty;

    public PortfolioProfile()
    {
    }

    /// <summary>
    /// Клонируем портфолио
    /// </summary>
    /// <param name="other">Портфолио-источник.</param>
    public PortfolioProfile(PortfolioProfile? other)
    {
        if (other is null)
            return;

        DistinguishingFeatures = other.DistinguishingFeatures;
        Region = other.Region;
        PlanetOrColony = other.PlanetOrColony;
        Address = other.Address;
        Education = other.Education;
        WorkExperience = other.WorkExperience;
        MaritalStatusValue = other.MaritalStatusValue;
        CloseRelatives = other.CloseRelatives;
        EmergencyContact = other.EmergencyContact;
        PhysiologicalTraits = other.PhysiologicalTraits;
        PsychologicalTraits = other.PsychologicalTraits;
        ArrestHistory = other.ArrestHistory;
        ConvictionHistory = other.ConvictionHistory;
    }

    /// <summary>
    /// Генерирует портфолио из пулов при первом создании персонажа.
    /// </summary>
    public static PortfolioProfile RandomForSpecies(ProtoId<SpeciesPrototype> species)
    {
        return new PortfolioProfile
        {
            DistinguishingFeatures = PortfolioGenerator.Generate(PortfolioPoolLust.DistinguishingFeatures, species),
            Region = PortfolioGenerator.Generate(PortfolioPoolLust.Region, species),
            PlanetOrColony = PortfolioGenerator.Generate(PortfolioPoolLust.PlanetOrColony, species),
            Address = PortfolioGenerator.Generate(PortfolioPoolLust.Address, species),
            Education = PortfolioGenerator.Generate(PortfolioPoolLust.Education, species),
            WorkExperience = PortfolioGenerator.Generate(PortfolioPoolLust.WorkExperience, species),
            MaritalStatusValue = PortfolioGenerator.PickEnum<MaritalStatus>(),
            CloseRelatives = PortfolioGenerator.Generate(PortfolioPoolLust.CloseRelatives, species),
            EmergencyContact = PortfolioGenerator.Generate(PortfolioPoolLust.EmergencyContact, species),
            PhysiologicalTraits = PortfolioGenerator.Generate(PortfolioPoolLust.PhysiologicalTraits, species),
            PsychologicalTraits = PortfolioGenerator.Generate(PortfolioPoolLust.PsychologicalTraits, species),
            ArrestHistory = PortfolioGenerator.Generate(PortfolioPoolLust.ArrestHistory, species),
            ConvictionHistory = PortfolioGenerator.Generate(PortfolioPoolLust.ConvictionHistory, species),
        };
    }

    /// <summary>
    /// Сравнивает портфолио по всем полям.
    /// </summary>
    /// <param name="other">Портфолио для сравнения.</param>
    /// <returns>True, если все поля совпадают.</returns>
    public bool Equals(PortfolioProfile? other)
    {
        return other is not null &&
               DistinguishingFeatures == other.DistinguishingFeatures &&
               Region == other.Region &&
               PlanetOrColony == other.PlanetOrColony &&
               Address == other.Address &&
               Education == other.Education &&
               WorkExperience == other.WorkExperience &&
               MaritalStatusValue == other.MaritalStatusValue &&
               CloseRelatives == other.CloseRelatives &&
               EmergencyContact == other.EmergencyContact &&
               PhysiologicalTraits == other.PhysiologicalTraits &&
               PsychologicalTraits == other.PsychologicalTraits &&
               ArrestHistory == other.ArrestHistory &&
               ConvictionHistory == other.ConvictionHistory;
    }

    /// <summary>
    /// Санитизирует поля портфолио, вызывается из EnsureValid() профиля.
    /// Лимиты должны совпадать с MaxLength в Content.Server.Database/_Lust/Model.Lust.cs.
    /// </summary>
    public void EnsureValid()
    {
        DistinguishingFeatures = ValidateEntry(DistinguishingFeatures, MaxTextLength);
        Education = ValidateEntry(Education, MaxTextLength);
        WorkExperience = ValidateEntry(WorkExperience, MaxTextLength);
        CloseRelatives = ValidateEntry(CloseRelatives, MaxTextLength);
        EmergencyContact = ValidateEntry(EmergencyContact, MaxTextLength);
        PhysiologicalTraits = ValidateEntry(PhysiologicalTraits, MaxTextLength);
        PsychologicalTraits = ValidateEntry(PsychologicalTraits, MaxTextLength);
        ArrestHistory = ValidateEntry(ArrestHistory, MaxTextLength);
        ConvictionHistory = ValidateEntry(ConvictionHistory, MaxTextLength);
        MaritalStatusValue = ValidateEntry(MaritalStatusValue);
        Region = ValidateEntry(Region, MaxShortTextLength);
        PlanetOrColony = ValidateEntry(PlanetOrColony, MaxShortTextLength);
        Address = ValidateEntry(Address, MaxShortTextLength);
    }

    private static MaritalStatus ValidateEntry(MaritalStatus maritalStatus)
    {
        if (!Enum.IsDefined(typeof(MaritalStatus), maritalStatus))
            return MaritalStatus.Single;
        return maritalStatus;
    }

    private static string ValidateEntry(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength];
    }

    /// <summary>
    /// Сравнение двух профилей
    /// </summary>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is PortfolioProfile other && Equals(other);
    }

    /// <summary>
    /// Получение хеша портфолио
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(DistinguishingFeatures);
        hashCode.Add(Region);
        hashCode.Add(PlanetOrColony);
        hashCode.Add(Address);
        hashCode.Add(Education);
        hashCode.Add(WorkExperience);
        hashCode.Add(MaritalStatusValue);
        hashCode.Add(CloseRelatives);
        hashCode.Add(EmergencyContact);
        hashCode.Add(PhysiologicalTraits);
        hashCode.Add(PsychologicalTraits);
        hashCode.Add(ArrestHistory);
        hashCode.Add(ConvictionHistory);
        return hashCode.ToHashCode();
    }


}

