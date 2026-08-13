using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Content.Shared._Lust.Smell.Prototypes;


namespace Content.Shared._Lust.Smell;

public static class ScentSignatureGenerator
{
    public static ScentSignature Generate(
        string seed,
        PersonalScentProfilePrototype profile)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));

        float hue = ((hash[0] << 8 ) | hash[1]) / 65535f;
        float saturation = 0.45f + hash[2] / 255f * 0.20f;
        float value = 0.75f + hash[3] / 255f * 0.15f;

        Color color = Color.FromHsv(new Vector4(hue, saturation, value, 1f));

        List<LocId> notes = new List<LocId>(profile.NotePools.Count);

        for (var index = 0; index < profile.NotePools.Count; index++)
        {
            var pool = profile.NotePools[index];

            if (pool.Notes.Count == 0)
                continue;
            var hashIndex = 4 + index;
            var noteIndex = hash[hashIndex] % pool.Notes.Count;
            notes.Add(pool.Notes[noteIndex]);
        }

        return new ScentSignature(color, notes);

    }

}
