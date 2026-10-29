using System.Collections.Generic;
using System.Linq;
using Content.Client.Options.UI;
using Robust.Shared.ContentPack;

namespace Content.Client._Sunrise.CombatMode;

public static class CombatHitFeedbackSoundCatalog
{
    public const string SoundsDirectory = "/Audio/Effects/HitSounds/";
    public const string DefaultHitSound = "hit";
    public const string DefaultCriticalSound = "critical";
    public const string DefaultKillSound = "kill";

    public static IReadOnlyCollection<string> GetSoundIds(IResourceManager resources)
    {
        return resources.ContentFindFiles(SoundsDirectory)
            .Where(path => path.Extension.Equals("ogg", System.StringComparison.OrdinalIgnoreCase))
            .Select(path => path.FilenameWithoutExtension)
            .OrderBy(id => id, System.StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyCollection<OptionDropDownCVar<string>.ValueOption> GetOptions(IResourceManager resources)
    {
        return GetSoundIds(resources)
            .Select(id => new OptionDropDownCVar<string>.ValueOption(id, id.Replace('_', ' ')))
            .ToArray();
    }

    public static string ResolveSoundId(string? candidate, IReadOnlyCollection<string> available, string fallback)
    {
        if (candidate != null && available.Contains(candidate))
            return candidate;

        if (available.Contains(fallback))
            return fallback;

        return available.FirstOrDefault() ?? fallback;
    }

    public static float NormalizeVolume(float volume) =>
        float.IsFinite(volume) ? System.Math.Clamp(volume, 0f, 1f) : 0.75f;

    public static string GetSoundPath(string soundId) => $"{SoundsDirectory}{soundId}.ogg";
}
