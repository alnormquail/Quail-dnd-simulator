namespace CopilotTest.Models;

/// <summary>A class/species feature on a character's sheet (e.g. "Aura of Protection").</summary>
public class CharacterFeature
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>Where it came from, e.g. "Paladin 6" or "Gnome".</summary>
    public string Source { get; set; } = string.Empty;
    public int LevelGained { get; set; } = 1;
}
