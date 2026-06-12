namespace CopilotTest.Models;

public class Spell
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 0;
    public string School { get; set; } = string.Empty;
    public string CastingTime { get; set; } = "1 action";
    public string Range { get; set; } = string.Empty;
    public string Components { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public bool Concentration { get; set; } = false;
    public bool IsRitual { get; set; } = false;
    public bool IsPrepared { get; set; } = true;
    public string Description { get; set; } = string.Empty;

    public string LevelDisplay => Level == 0 ? "Cantrip" : $"Level {Level}";
}
