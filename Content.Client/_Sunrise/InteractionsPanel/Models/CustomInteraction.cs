namespace Content.Client._Sunrise.InteractionsPanel.Models;

// Lust-Edit: расширение модели кастомного действия в _Lust.
public sealed partial class CustomInteraction
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public List<string> InteractionMessages { get; set; } = new();
    public List<string> SoundIds { get; set; } = new();
    public bool SpawnsEffect { get; set; }
    public float EffectChance { get; set; }
    public string EntityEffectId { get; set; } = string.Empty;
    public float Cooldown { get; set; } = 3.0f;

}
