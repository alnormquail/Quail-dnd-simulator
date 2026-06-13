namespace CopilotTest.Models.Content;

/// <summary>Which rules edition a piece of content comes from.</summary>
public enum RulesEdition { Edition2014, Edition2024 }

/// <summary>A single ability score bonus granted by content (e.g. +2 Dexterity).</summary>
public record AbilityBonus(AbilityScore Ability, int Amount);

/// <summary>A named trait/feature granted by content, shown on the sheet.</summary>
public record ContentTrait(string Name, string Description);

/// <summary>
/// A playable species/race option. Subraces are modeled as their own flat
/// entries (e.g. "High Elf", "Hill Dwarf") that already include the base
/// species' traits — this keeps selection to a single dropdown.
/// </summary>
public record SpeciesData
{
    public string Key { get; init; } = "";          // slug, e.g. "high-elf"
    public string Name { get; init; } = "";          // display, e.g. "High Elf"
    public string Source { get; init; } = "PHB 2024"; // book
    public RulesEdition Edition { get; init; } = RulesEdition.Edition2024;

    public string Size { get; init; } = "Medium";
    public int Speed { get; init; } = 30;
    public int DarkvisionFt { get; init; } = 0;       // 0 = none

    public IReadOnlyList<AbilityBonus> AbilityBonuses { get; init; } = [];
    public IReadOnlyList<ContentTrait> Traits { get; init; } = [];
    public IReadOnlyList<Skill> SkillProficiencies { get; init; } = [];
    public IReadOnlyList<string> Languages { get; init; } = [];

    /// <summary>Short summary line for the picker (e.g. "+2 DEX, +1 INT · Darkvision").</summary>
    public string Summary
    {
        get
        {
            var bonuses = AbilityBonuses.Count > 0
                ? string.Join(", ", AbilityBonuses.Select(b => $"{Fmt(b.Amount)} {Short(b.Ability)}"))
                : "no ability bonus";
            var extras = DarkvisionFt > 0 ? " · Darkvision" : "";
            return $"{bonuses}{extras}";
        }
    }

    private static string Fmt(int n) => n >= 0 ? $"+{n}" : n.ToString();
    private static string Short(AbilityScore a) => a switch
    {
        AbilityScore.Strength => "STR", AbilityScore.Dexterity => "DEX",
        AbilityScore.Constitution => "CON", AbilityScore.Intelligence => "INT",
        AbilityScore.Wisdom => "WIS", AbilityScore.Charisma => "CHA", _ => "?"
    };
}
