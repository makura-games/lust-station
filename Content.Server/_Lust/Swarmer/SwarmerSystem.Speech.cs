using Content.Server._Lust.Swarmer.Components;
using Content.Shared.Chat;

namespace Content.Server._Lust.Swarmer;

public sealed partial class SwarmerSystem
{
    private void InitializeSpeech()
    {
        SubscribeLocalEvent<SwarmerSpeechSoundsComponent, EntitySpokeEvent>(OnSpoke);
    }

    /// <summary>
    /// Plays the sound of whichever phrase the accent turned the swarmer's speech into.
    /// </summary>
    private void OnSpoke(Entity<SwarmerSpeechSoundsComponent> ent, ref EntitySpokeEvent args)
    {
        foreach (var (phrase, sound) in ent.Comp.Sounds)
        {
            if (args.Message != Loc.GetString(phrase))
                continue;

            _audio.PlayPvs(sound, ent);
            return;
        }
    }
}
