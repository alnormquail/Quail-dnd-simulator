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
    private static string Short(AbilityScore a) => AbilityNames.Short(a);
}

/// <summary>Shared ability-name helpers.</summary>
public static class AbilityNames
{
    public static string Short(AbilityScore a) => a switch
    {
        AbilityScore.Strength => "STR", AbilityScore.Dexterity => "DEX",
        AbilityScore.Constitution => "CON", AbilityScore.Intelligence => "INT",
        AbilityScore.Wisdom => "WIS", AbilityScore.Charisma => "CHA", _ => "?"
    };
}

public enum FeatCategory { Origin, General, FightingStyle, EpicBoon }

/// <summary>A subclass feature gained at a specific class level.</summary>
public record SubclassFeature(int Level, string Name, string Description);

/// <summary>A subclass (e.g. "College of Lore") belonging to a class.</summary>
public record SubclassData
{
    public string Key { get; init; } = "";
    public string Name { get; init; } = "";
    /// <summary>Lowercase class name this subclass belongs to (e.g. "bard").</summary>
    public string ClassName { get; init; } = "";
    public string Source { get; init; } = "PHB 2024";
    public RulesEdition Edition { get; init; } = RulesEdition.Edition2024;
    public IReadOnlyList<SubclassFeature> Features { get; init; } = [];
    /// <summary>Spell names this subclass grants (always-prepared). Granted when castable; removed on swap.</summary>
    public IReadOnlyList<string> GrantedSpells { get; init; } = [];
}

/// <summary>
/// A 2024 background: ability bonuses are allocated by the player among three
/// listed abilities (+2/+1 or +1/+1/+1), plus skills, a tool, and an Origin feat.
/// </summary>
public record BackgroundData
{
    public string Key { get; init; } = "";
    public string Name { get; init; } = "";
    public string Source { get; init; } = "PHB 2024";
    public RulesEdition Edition { get; init; } = RulesEdition.Edition2024;

    /// <summary>The three abilities a player may boost (choose +2/+1 or +1/+1/+1).</summary>
    public IReadOnlyList<AbilityScore> AbilityOptions { get; init; } = [];
    public IReadOnlyList<Skill> SkillProficiencies { get; init; } = [];
    public string ToolProficiency { get; init; } = "";
    public string OriginFeatKey { get; init; } = "";
    public string Equipment { get; init; } = "";
    public string Description { get; init; } = "";

    public string AbilitySummary =>
        string.Join("/", AbilityOptions.Select(AbilityNames.Short));
}

/// <summary>A feat (Origin or General). Effects are shown as traits; some carry fixed ability bumps.</summary>
public record FeatData
{
    public string Key { get; init; } = "";
    public string Name { get; init; } = "";
    public string Source { get; init; } = "PHB 2024";
    public RulesEdition Edition { get; init; } = RulesEdition.Edition2024;

    public FeatCategory Category { get; init; } = FeatCategory.Origin;
    public string Prerequisite { get; init; } = "";
    /// <summary>Fixed ability increases auto-applied (choice-based bumps are described instead).</summary>
    public IReadOnlyList<AbilityBonus> AbilityBonuses { get; init; } = [];
    public IReadOnlyList<ContentTrait> Traits { get; init; } = [];

    public string CategoryLabel => Category switch
    {
        FeatCategory.Origin => "Origin Feat",
        FeatCategory.General => "General Feat",
        FeatCategory.FightingStyle => "Fighting Style",
        FeatCategory.EpicBoon => "Epic Boon",
        _ => "Feat"
    };
}
