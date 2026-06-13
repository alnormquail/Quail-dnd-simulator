namespace CopilotTest.Models.Content;

/// <summary>
/// Per-class build data (2024 PHB): saving-throw proficiencies and the skill
/// options a class chooses from at level 1. Combat data (hit die, spell slots,
/// feature grants) lives in <see cref="LevelUpRules"/>.
/// </summary>
public record ClassData
{
    public string Key { get; init; } = "";      // lowercase class name
    public string Name { get; init; } = "";
    public string Source { get; init; } = "PHB 2024";
    public IReadOnlyList<AbilityScore> SaveProficiencies { get; init; } = [];
    public int SkillChoiceCount { get; init; } = 2;
    public IReadOnlyList<Skill> SkillOptions { get; init; } = [];
    public string PrimaryAbility { get; init; } = "";  // guidance text
}

public static class ClassLibrary
{
    public static IReadOnlyList<ClassData> All => _all;
    public static IReadOnlyList<string> Names => _all.Select(c => c.Name).ToList();

    public static ClassData? Get(string? className) =>
        className is null ? null
        : _all.FirstOrDefault(c => c.Key == className.Trim().ToLowerInvariant());

    private static ClassData C(string key, string name, AbilityScore s1, AbilityScore s2,
        int skillCount, string primary, params Skill[] skills) =>
        new() { Key = key, Name = name, SaveProficiencies = [s1, s2],
                SkillChoiceCount = skillCount, SkillOptions = skills, PrimaryAbility = primary };

    private static readonly List<ClassData> _all =
    [
        C("barbarian", "Barbarian", AbilityScore.Strength, AbilityScore.Constitution, 2, "Strength",
            Skill.AnimalHandling, Skill.Athletics, Skill.Intimidation, Skill.Nature, Skill.Perception, Skill.Survival),
        C("bard", "Bard", AbilityScore.Dexterity, AbilityScore.Charisma, 3, "Charisma",
            Skill.Acrobatics, Skill.AnimalHandling, Skill.Arcana, Skill.Athletics, Skill.Deception, Skill.History,
            Skill.Insight, Skill.Intimidation, Skill.Investigation, Skill.Medicine, Skill.Nature, Skill.Perception,
            Skill.Performance, Skill.Persuasion, Skill.Religion, Skill.SleightOfHand, Skill.Stealth, Skill.Survival),
        C("cleric", "Cleric", AbilityScore.Wisdom, AbilityScore.Charisma, 2, "Wisdom",
            Skill.History, Skill.Insight, Skill.Medicine, Skill.Persuasion, Skill.Religion),
        C("druid", "Druid", AbilityScore.Intelligence, AbilityScore.Wisdom, 2, "Wisdom",
            Skill.Arcana, Skill.AnimalHandling, Skill.Insight, Skill.Medicine, Skill.Nature, Skill.Perception,
            Skill.Religion, Skill.Survival),
        C("fighter", "Fighter", AbilityScore.Strength, AbilityScore.Constitution, 2, "Strength or Dexterity",
            Skill.Acrobatics, Skill.AnimalHandling, Skill.Athletics, Skill.History, Skill.Insight,
            Skill.Intimidation, Skill.Perception, Skill.Persuasion, Skill.Survival),
        C("monk", "Monk", AbilityScore.Strength, AbilityScore.Dexterity, 2, "Dexterity & Wisdom",
            Skill.Acrobatics, Skill.Athletics, Skill.History, Skill.Insight, Skill.Religion, Skill.Stealth),
        C("paladin", "Paladin", AbilityScore.Wisdom, AbilityScore.Charisma, 2, "Strength & Charisma",
            Skill.Athletics, Skill.Insight, Skill.Intimidation, Skill.Medicine, Skill.Persuasion, Skill.Religion),
        C("ranger", "Ranger", AbilityScore.Strength, AbilityScore.Dexterity, 3, "Dexterity & Wisdom",
            Skill.AnimalHandling, Skill.Athletics, Skill.Insight, Skill.Investigation, Skill.Nature,
            Skill.Perception, Skill.Stealth, Skill.Survival),
        C("rogue", "Rogue", AbilityScore.Dexterity, AbilityScore.Intelligence, 4, "Dexterity",
            Skill.Acrobatics, Skill.Athletics, Skill.Deception, Skill.Insight, Skill.Intimidation,
            Skill.Investigation, Skill.Perception, Skill.Performance, Skill.Persuasion, Skill.SleightOfHand, Skill.Stealth),
        C("sorcerer", "Sorcerer", AbilityScore.Constitution, AbilityScore.Charisma, 2, "Charisma",
            Skill.Arcana, Skill.Deception, Skill.Insight, Skill.Intimidation, Skill.Persuasion, Skill.Religion),
        C("warlock", "Warlock", AbilityScore.Wisdom, AbilityScore.Charisma, 2, "Charisma",
            Skill.Arcana, Skill.Deception, Skill.History, Skill.Intimidation, Skill.Investigation, Skill.Nature, Skill.Religion),
        C("wizard", "Wizard", AbilityScore.Intelligence, AbilityScore.Wisdom, 2, "Intelligence",
            Skill.Arcana, Skill.History, Skill.Insight, Skill.Investigation, Skill.Medicine, Skill.Nature, Skill.Religion),
    ];
}
