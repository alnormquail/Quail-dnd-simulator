namespace CopilotTest.Models;

/// <summary>
/// Hard-coded character templates for the three alnormquail PDF characters.
/// These are pre-seeded into the SavedRoster so they appear without uploading.
/// </summary>
public static class PreloadedCharacters
{
    public static IReadOnlyList<Combatant> All => [Spurt, Belqorel, Wally];

    /// <summary>Spurt the Sorcerer — Kobold Sorcerer 3</summary>
    public static Combatant Spurt => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000001"),
        Name           = "Spurt the Sorcerer",
        Type           = CombatantType.PC,
        CharacterClass = "Sorcerer",
        CharacterLevel = 3,

        Strength     = 7,  Dexterity    = 14, Constitution = 12,
        Intelligence = 12, Wisdom       = 10, Charisma     = 16,

        MaxHitPoints     = 17, CurrentHitPoints = 17,
        ArmorClass       = 12,
        Speed            = 30,
        ProficiencyBonus = 2,

        Actions = new List<CombatAction>
        {
            new() { Name = "Dagger",          ActionType = ActionType.Attack,      AttackBonus = 4,  DamageDice = "1d4",  DamageBonus = 2,  DamageType = DamageType.Piercing,   Range = "20 ft" },
            new() { Name = "Scorpion Staff",  ActionType = ActionType.Attack,      AttackBonus = 4,  DamageDice = "1d6",  DamageBonus = 2,  DamageType = DamageType.Poison,     Range = "5 ft"  },
            new() { Name = "Shocking Grasp",  ActionType = ActionType.SpellAttack, AttackBonus = 5,  DamageDice = "1d8",  DamageBonus = 0,  DamageType = DamageType.Lightning,  Range = "Touch", SpellLevel = 0, UsesPerDay = 0, UsesRemaining = 0 },
            new() { Name = "Sorcerous Burst", ActionType = ActionType.SpellAttack, AttackBonus = 5,  DamageDice = "1d8",  DamageBonus = 0,  DamageType = DamageType.Acid,       Range = "120 ft", SpellLevel = 0, UsesPerDay = 0, UsesRemaining = 0 },
            new() { Name = "Burning Hands",   ActionType = ActionType.Spell,       SaveDC = 13, SaveAbility = AbilityScore.Dexterity,     DamageDice = "3d6",  DamageType = DamageType.Fire,   Range = "15 ft Cone", SpellLevel = 1, UsesPerDay = 4, UsesRemaining = 4 },
            new() { Name = "Color Spray",     ActionType = ActionType.Spell,       SaveDC = 13, SaveAbility = AbilityScore.Constitution,  DamageDice = "2d6",  DamageType = DamageType.None,   Range = "15 ft Cone", SpellLevel = 1, UsesPerDay = 4, UsesRemaining = 4 },
            new() { Name = "Dragon's Breath", ActionType = ActionType.Spell,       SaveDC = 13, SaveAbility = AbilityScore.Dexterity,     DamageDice = "3d6",  DamageType = DamageType.Fire,   Range = "15 ft Cone", SpellLevel = 2, UsesPerDay = 2, UsesRemaining = 2 },
        }
    };

    /// <summary>Belqorel — Dwarf Barbarian 5</summary>
    public static Combatant Belqorel => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000002"),
        Name           = "Belqorel",
        Type           = CombatantType.PC,
        CharacterClass = "Barbarian",
        CharacterLevel = 5,

        Strength     = 15, Dexterity    = 16, Constitution = 14,
        Intelligence = 10, Wisdom       = 8,  Charisma     = 12,

        MaxHitPoints     = 55, CurrentHitPoints = 55,
        ArmorClass       = 15,
        Speed            = 40,
        ProficiencyBonus = 3,

        IsBarbarianClass  = true,
        RageBonus         = 2,
        RageUsesPerDay    = 3,
        RageUsesRemaining = 3,

        Actions = new List<CombatAction>
        {
            new() { Name = "Dagger",          ActionType = ActionType.Attack, AttackBonus = 6, DamageDice = "1d4",  DamageBonus = 3, DamageType = DamageType.Piercing, Range = "20 ft" },
            new() { Name = "Greataxe",        ActionType = ActionType.Attack, AttackBonus = 5, DamageDice = "1d12", DamageBonus = 2, DamageType = DamageType.Slashing, Range = "5 ft"  },
            new() { Name = "Handaxe",         ActionType = ActionType.Attack, AttackBonus = 5, DamageDice = "1d6",  DamageBonus = 2, DamageType = DamageType.Slashing, Range = "20 ft" },
            new() { Name = "Unarmed Strike",  ActionType = ActionType.Attack, AttackBonus = 5, DamageDice = "1",    DamageBonus = 3, DamageType = DamageType.Bludgeoning, Range = "5 ft" },
        }
    };

    /// <summary>Wally Cornbone — Halfling Rogue 4</summary>
    public static Combatant Wally => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000003"),
        Name           = "Wally Cornbone",
        Type           = CombatantType.PC,
        CharacterClass = "Rogue",
        CharacterLevel = 4,

        Strength     = 8,  Dexterity    = 15, Constitution = 12,
        Intelligence = 15, Wisdom       = 12, Charisma     = 13,

        MaxHitPoints     = 27, CurrentHitPoints = 27,
        ArmorClass       = 13,
        Speed            = 30,
        ProficiencyBonus = 2,

        Actions = new List<CombatAction>
        {
            new() { Name = "Dagger",      ActionType = ActionType.Attack, AttackBonus = 4, DamageDice = "1d4",  DamageBonus = 2, DamageType = DamageType.Piercing, Range = "20 ft" },
            new() { Name = "Seeker Dart", ActionType = ActionType.Attack, AttackBonus = 4, DamageDice = "1d4",  DamageBonus = 2, DamageType = DamageType.Piercing, Range = "20 ft" },
            new() { Name = "Blowgun",     ActionType = ActionType.Attack, AttackBonus = 2, DamageDice = "1",    DamageBonus = 3, DamageType = DamageType.Piercing, Range = "25 ft" },
        }
    };
}
