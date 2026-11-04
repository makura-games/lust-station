using Robust.Shared.Audio;

namespace Content.Server._Lust.Swarmer.Components;

/// <summary>
/// Plays a sound matching each phrase a swarmer can say out loud.
/// Its speech is fully replaced by an accent, so every spoken line is one of these phrases.
/// </summary>
[RegisterComponent]
public sealed partial class SwarmerSpeechSoundsComponent : Component
{
    /// <summary>
    /// Sound played for each phrase, keyed by the phrase's localization ID.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<LocId, SoundSpecifier> Sounds = [];
}
