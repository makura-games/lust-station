using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Smell;

/// <summary>
/// A character's personal scent: color and notes deterministically generated
/// from the species profile and the character's seed. Same seed — same signature.
/// </summary>
public sealed record ScentSignature(Color Color, IReadOnlyList<LocId> Notes);
