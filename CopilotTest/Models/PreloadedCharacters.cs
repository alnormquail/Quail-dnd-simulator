namespace CopilotTest.Models;

/// <summary>
/// Hard-coded character templates for the three party members.
/// Used to seed the database on first run.
/// </summary>
public static class PreloadedCharacters
{
    public static IReadOnlyList<Character> All => [Spurt, Belqorel, Wally, Winnie, Kennyth, Boan, Gideon, Job, Bren];

    /// <summary>Builds Spell entries from the shared library by name (skips unknowns).</summary>
    private static List<Spell> SpellsFor(params string[] names) =>
        names.Select(n => SpellLibrary.All.FirstOrDefault(s => s.Name == n))
             .Where(s => s != null)
             .Select(s => SpellLibrary.ToSpell(s!, Guid.Empty))
             .ToList();

    private static InventoryItem Inv(string name, int qty, ItemCategory cat, bool equipped = false) =>
        new() { Name = name, Quantity = qty, Category = cat, IsEquipped = equipped };

    public static Character Spurt => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000001"),
        Name           = "Spurt the Sorcerer",
        Type           = CombatantType.PC,
        CharacterClass = "Sorcerer",
        CharacterLevel = 3,
        Race           = "Kobold",

        Strength     = 7,  Dexterity    = 14, Constitution = 12,
        Intelligence = 12, Wisdom       = 10, Charisma     = 16,

        MaxHitPoints     = 17,
        ArmorClass       = 12,
        Speed            = 30,
        ProficiencyBonus = 2,

        SaveProfConstitution = true,
        SaveProfCharisma     = true,

        Actions = new List<CombatAction>
        {
            new() { Name = "Dagger",          ActionType = ActionType.Attack,      AttackBonus = 4, DamageDice = "1d4",  DamageBonus = 2,  DamageType = DamageType.Piercing,   Range = "20 ft" },
            new() { Name = "Scorpion Staff",  ActionType = ActionType.Attack,      AttackBonus = 4, DamageDice = "1d6",  DamageBonus = 2,  DamageType = DamageType.Poison,     Range = "5 ft"  },
            new() { Name = "Shocking Grasp",  ActionType = ActionType.SpellAttack, AttackBonus = 5, DamageDice = "1d8",  DamageBonus = 0,  DamageType = DamageType.Lightning,  Range = "Touch",  SpellLevel = 0 },
            new() { Name = "Sorcerous Burst", ActionType = ActionType.SpellAttack, AttackBonus = 5, DamageDice = "1d8",  DamageBonus = 0,  DamageType = DamageType.Acid,       Range = "120 ft", SpellLevel = 0 },
            new() { Name = "Burning Hands",   ActionType = ActionType.Spell,       SaveDC = 13, SaveAbility = AbilityScore.Dexterity,    DamageDice = "3d6",  DamageType = DamageType.Fire,  Range = "15 ft Cone", SpellLevel = 1, UsesPerDay = 4, UsesRemaining = 4 },
            new() { Name = "Color Spray",     ActionType = ActionType.Spell,       SaveDC = 13, SaveAbility = AbilityScore.Constitution, DamageDice = "2d6",  DamageType = DamageType.None,  Range = "15 ft Cone", SpellLevel = 1, UsesPerDay = 4, UsesRemaining = 4 },
            new() { Name = "Dragon's Breath", ActionType = ActionType.Spell,       SaveDC = 13, SaveAbility = AbilityScore.Dexterity,    DamageDice = "3d6",  DamageType = DamageType.Fire,  Range = "15 ft Cone", SpellLevel = 2, UsesPerDay = 2, UsesRemaining = 2 },
        },

        Spells = new List<Spell>
        {
            new() { Name = "Shocking Grasp",  Level = 0, School = "Evocation",    CastingTime = "1 action", Range = "Touch",      Components = "V, S",    Duration = "Instantaneous", Description = "Lightning springs from your hand to deliver a shock to a creature. Make a melee spell attack. On a hit, the target takes 1d8 lightning damage. The target can't take reactions until the start of its next turn." },
            new() { Name = "Sorcerous Burst", Level = 0, School = "Evocation",    CastingTime = "1 action", Range = "120 ft",     Components = "V, S",    Duration = "Instantaneous", Description = "You cast a burst of magical energy at one creature within range. On a hit, the target takes 1d8 acid damage." },
            new() { Name = "Burning Hands",   Level = 1, School = "Evocation",    CastingTime = "1 action", Range = "Self (15 ft cone)", Components = "V, S", Duration = "Instantaneous", Description = "As you hold your hands with thumbs touching and fingers spread, a thin sheet of flames shoots forth. Each creature in a 15-foot cone must make a DC 13 Dexterity saving throw. A creature takes 3d6 fire damage on a failed save, or half as much on a successful one." },
            new() { Name = "Color Spray",     Level = 1, School = "Illusion",     CastingTime = "1 action", Range = "Self (15 ft cone)", Components = "V, S, M", Duration = "1 round", Description = "A dazzling array of flashing, colored light springs from your hand. Roll 6d10; the total is how many hit points of creatures this spell can affect." },
            new() { Name = "Dragon's Breath", Level = 2, School = "Transmutation",CastingTime = "1 bonus action", Range = "Touch", Components = "V, S, M", Duration = "1 minute", Concentration = true, Description = "You touch one willing creature and imbue it with the power to spew magical energy from its mouth. Until the spell ends, the creature can use an action to exhale energy of a type: fire, cold, lightning, acid, or poison. Each creature in a 15-foot cone must make a DC 13 Dexterity saving throw, taking 3d6 damage on a failed save." },
        },

        SpellSlots = new List<SpellSlot>
        {
            new() { Level = 1, MaxSlots = 4, UsedSlots = 0 },
            new() { Level = 2, MaxSlots = 2, UsedSlots = 0 },
        },

        Skills = new List<CharacterSkill>
        {
            new() { Skill = Skill.Arcana,     Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Deception,  Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Persuasion, Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Stealth,    Proficiency = ProficiencyLevel.Proficient },
        },
    };

    public static Character Belqorel => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000002"),
        Name           = "Belqorel",
        Type           = CombatantType.PC,
        CharacterClass = "Barbarian",
        CharacterLevel = 5,
        Race           = "Dwarf",

        Strength     = 15, Dexterity    = 16, Constitution = 14,
        Intelligence = 10, Wisdom       = 8,  Charisma     = 12,

        MaxHitPoints     = 55,
        ArmorClass       = 15,
        Speed            = 40,
        ProficiencyBonus = 3,

        SaveProfStrength     = true,
        SaveProfConstitution = true,

        IsBarbarianClass  = true,
        RageBonus         = 2,
        RageUsesPerDay    = 3,

        Actions = new List<CombatAction>
        {
            new() { Name = "Dagger",         ActionType = ActionType.Attack, AttackBonus = 6, DamageDice = "1d4",  DamageBonus = 3, DamageType = DamageType.Piercing,    Range = "20 ft" },
            new() { Name = "Greataxe",       ActionType = ActionType.Attack, AttackBonus = 5, DamageDice = "1d12", DamageBonus = 2, DamageType = DamageType.Slashing,    Range = "5 ft"  },
            new() { Name = "Handaxe",        ActionType = ActionType.Attack, AttackBonus = 5, DamageDice = "1d6",  DamageBonus = 2, DamageType = DamageType.Slashing,    Range = "20 ft" },
            new() { Name = "Unarmed Strike", ActionType = ActionType.Attack, AttackBonus = 5, DamageDice = "1",    DamageBonus = 3, DamageType = DamageType.Bludgeoning, Range = "5 ft"  },
        },

        Skills = new List<CharacterSkill>
        {
            new() { Skill = Skill.Athletics,    Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Intimidation, Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Perception,   Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Survival,     Proficiency = ProficiencyLevel.Proficient },
        },

        Inventory = new List<InventoryItem>
        {
            new() { Name = "Greataxe",  Quantity = 1, IsEquipped = true,  Category = ItemCategory.Weapon,  Description = "Two-handed axe" },
            new() { Name = "Handaxe",   Quantity = 2, IsEquipped = true,  Category = ItemCategory.Weapon },
            new() { Name = "Dagger",    Quantity = 1, IsEquipped = true,  Category = ItemCategory.Weapon },
            new() { Name = "Explorer's Pack", Quantity = 1, Category = ItemCategory.Other },
        },

        GoldPieces = 15,
    };

    public static Character Wally => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000003"),
        Name           = "Wally Cornbone",
        Type           = CombatantType.PC,
        CharacterClass = "Rogue",
        CharacterLevel = 4,
        Race           = "Halfling",

        Strength     = 8,  Dexterity    = 15, Constitution = 12,
        Intelligence = 15, Wisdom       = 12, Charisma     = 13,

        MaxHitPoints     = 27,
        ArmorClass       = 13,
        Speed            = 30,
        ProficiencyBonus = 2,

        SaveProfDexterity    = true,
        SaveProfIntelligence = true,

        Actions = new List<CombatAction>
        {
            new() { Name = "Dagger",      ActionType = ActionType.Attack, AttackBonus = 4, DamageDice = "1d4",  DamageBonus = 2, DamageType = DamageType.Piercing, Range = "20 ft" },
            new() { Name = "Seeker Dart", ActionType = ActionType.Attack, AttackBonus = 4, DamageDice = "1d4",  DamageBonus = 2, DamageType = DamageType.Piercing, Range = "20 ft" },
            new() { Name = "Blowgun",     ActionType = ActionType.Attack, AttackBonus = 2, DamageDice = "1",    DamageBonus = 3, DamageType = DamageType.Piercing, Range = "25 ft" },
        },

        Skills = new List<CharacterSkill>
        {
            new() { Skill = Skill.Acrobatics,   Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Deception,    Proficiency = ProficiencyLevel.Expertise  },
            new() { Skill = Skill.Investigation,Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Perception,   Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.SleightOfHand,Proficiency = ProficiencyLevel.Expertise  },
            new() { Skill = Skill.Stealth,      Proficiency = ProficiencyLevel.Proficient },
        },

        Inventory = new List<InventoryItem>
        {
            new() { Name = "Dagger",      Quantity = 2, IsEquipped = true,  Category = ItemCategory.Weapon },
            new() { Name = "Seeker Dart", Quantity = 5, IsEquipped = false, Category = ItemCategory.Weapon },
            new() { Name = "Blowgun",     Quantity = 1, IsEquipped = true,  Category = ItemCategory.Weapon },
            new() { Name = "Thieves' Tools", Quantity = 1, Category = ItemCategory.Tool, Description = "Proficient" },
            new() { Name = "Burglar's Pack", Quantity = 1, Category = ItemCategory.Other },
        },

        GoldPieces = 22,
        SilverPieces = 10,
    };

    public static Character Winnie => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000004"),
        Name           = "Winnie Vale",
        Type           = CombatantType.PC,
        CharacterClass = "Sorcerer",
        CharacterLevel = 5,
        Race           = "High Elf",
        Background     = "Noble",
        Subclass       = "Wild Magic",

        Strength     = 8,  Dexterity    = 13, Constitution = 14,
        Intelligence = 14, Wisdom       = 10, Charisma     = 18,

        MaxHitPoints     = 32,
        ArmorClass       = 11,
        Speed            = 30,
        ProficiencyBonus = 3,

        SaveProfConstitution = true,
        SaveProfCharisma     = true,

        Actions = new List<CombatAction>
        {
            new() { Name = "Fire Bolt",    ActionType = ActionType.SpellAttack, AttackBonus = 7, DamageDice = "2d10", DamageBonus = 0, DamageType = DamageType.Fire,      Range = "120 ft", SpellLevel = 0 },
            new() { Name = "Chill Touch",  ActionType = ActionType.SpellAttack, AttackBonus = 7, DamageDice = "2d10", DamageBonus = 0, DamageType = DamageType.Necrotic,  Range = "120 ft", SpellLevel = 0 },
            new() { Name = "Thunderwave",  ActionType = ActionType.Spell,       SaveDC = 15, SaveAbility = AbilityScore.Constitution, DamageDice = "2d8", DamageType = DamageType.Thunder, Range = "15 ft Cube", SpellLevel = 1, UsesPerDay = 4, UsesRemaining = 4 },
            new() { Name = "Sleep",        ActionType = ActionType.Spell,       SaveDC = 15, SaveAbility = AbilityScore.Wisdom,       DamageDice = "5d8",  DamageType = DamageType.None,   Range = "90 ft",      SpellLevel = 1, UsesPerDay = 4, UsesRemaining = 4 },
            new() { Name = "Hold Person",  ActionType = ActionType.Spell,       SaveDC = 15, SaveAbility = AbilityScore.Wisdom,       DamageDice = "",     DamageType = DamageType.None,   Range = "60 ft",      SpellLevel = 2, UsesPerDay = 3, UsesRemaining = 3 },
            new() { Name = "Aganazzar's Scorcher", ActionType = ActionType.Spell, SaveDC = 15, SaveAbility = AbilityScore.Dexterity, DamageDice = "3d8", DamageType = DamageType.Fire, Range = "30 ft Line", SpellLevel = 2, UsesPerDay = 3, UsesRemaining = 3 },
        },

        SpellSlots = new List<SpellSlot>
        {
            new() { Level = 1, MaxSlots = 4, UsedSlots = 0 },
            new() { Level = 2, MaxSlots = 3, UsedSlots = 0 },
        },

        Skills = new List<CharacterSkill>
        {
            new() { Skill = Skill.Deception,   Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.History,      Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Perception,   Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Performance,  Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Persuasion,   Proficiency = ProficiencyLevel.Proficient },
        },

        Spells = SpellsFor(
            "Prestidigitation", "Minor Illusion", "Friends", "Fire Bolt", "Chill Touch",
            "Shield", "Sleep", "Thunderwave",
            "Hold Person", "Aganazzar's Scorcher"),

        Inventory = new List<InventoryItem>
        {
            Inv("Dagger", 2, ItemCategory.Weapon, true),
            Inv("Spear", 1, ItemCategory.Weapon, true),
            Inv("Crystal (Arcane Focus)", 1, ItemCategory.Other, true),
            Inv("Backpack", 1, ItemCategory.Other),
            Inv("Rations", 10, ItemCategory.Consumable),
            Inv("Oil", 2, ItemCategory.Consumable),
            Inv("Torch", 10, ItemCategory.Other),
            Inv("Rope (50 ft)", 1, ItemCategory.Other),
            Inv("Waterskin", 1, ItemCategory.Other),
            Inv("Tinderbox", 1, ItemCategory.Other),
            Inv("Caltrops", 20, ItemCategory.Other),
            Inv("Crowbar", 1, ItemCategory.Other),
        },

        PersonalityTraits = "Winnie Vale — Wild Magic Sorcerer 5, High Elf Noble.",
        GoldPieces = 50,
    };

    public static Character Kennyth => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000005"),
        Name           = "Kennyth",
        Type           = CombatantType.PC,
        CharacterClass = "Paladin",
        CharacterLevel = 5,
        Race           = "Gnome",
        Background     = "Custom",

        Strength     = 10, Dexterity    = 15, Constitution = 15,
        Intelligence = 15, Wisdom       = 12, Charisma     = 10,

        MaxHitPoints     = 44,
        ArmorClass       = 12,
        Speed            = 30,
        ProficiencyBonus = 3,

        SaveProfWisdom   = true,
        SaveProfCharisma = true,

        Actions = new List<CombatAction>
        {
            new() { Name = "Greatsword",    ActionType = ActionType.Attack, AttackBonus = 3, DamageDice = "2d6",  DamageBonus = 0, DamageType = DamageType.Slashing, Range = "5 ft" },
            new() { Name = "Divine Smite",  ActionType = ActionType.Spell,  AttackBonus = 0, DamageDice = "2d8",  DamageBonus = 0, DamageType = DamageType.Radiant,  Range = "Self", SpellLevel = 1, UsesPerDay = 4, UsesRemaining = 4 },
            new() { Name = "Shield of Faith", ActionType = ActionType.Spell, SaveDC = 0,     DamageDice = "",     DamageType = DamageType.None, Range = "60 ft",   SpellLevel = 1, UsesPerDay = 4, UsesRemaining = 4 },
            new() { Name = "Aid",           ActionType = ActionType.Spell,  SaveDC = 0,      DamageDice = "",     DamageType = DamageType.None, Range = "30 ft",   SpellLevel = 2, UsesPerDay = 2, UsesRemaining = 2 },
        },

        SpellSlots = new List<SpellSlot>
        {
            new() { Level = 1, MaxSlots = 4, UsedSlots = 0 },
            new() { Level = 2, MaxSlots = 2, UsedSlots = 0 },
        },

        Skills = new List<CharacterSkill>
        {
            new() { Skill = Skill.Athletics, Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Medicine,  Proficiency = ProficiencyLevel.Proficient },
        },

        Spells = SpellsFor(
            "Mending", "Prestidigitation", "Spare the Dying", "Guidance",
            "Protection from Evil and Good", "Shield of Faith",
            "Aid", "Zone of Truth", "Find Steed"),

        Inventory = new List<InventoryItem>
        {
            Inv("Greatsword", 1, ItemCategory.Weapon, true),
        },

        PersonalityTraits = "Kennyth — Paladin 5, Gnome. Lay on Hands pool: 25 HP. Gnome Cunning: Advantage on INT/WIS/CHA saves vs magic.",
        GoldPieces = 30,
    };

    public static Character Boan => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000006"),
        Name           = "Boan Strickler",
        Type           = CombatantType.PC,
        CharacterClass = "Fighter",
        CharacterLevel = 5,
        Race           = "Gnome",
        Background     = "Folk Hero",

        Strength     = 17, Dexterity    = 15, Constitution = 13,
        Intelligence = 8,  Wisdom       = 12, Charisma     = 10,

        MaxHitPoints     = 39,
        ArmorClass       = 14,
        Speed            = 30,
        ProficiencyBonus = 3,

        SaveProfStrength     = true,
        SaveProfConstitution = true,

        Actions = new List<CombatAction>
        {
            new() { Name = "Handaxe",        ActionType = ActionType.Attack, AttackBonus = 6, DamageDice = "1d6",  DamageBonus = 3, DamageType = DamageType.Slashing,    Range = "20 ft"  },
            new() { Name = "Scimitar",        ActionType = ActionType.Attack, AttackBonus = 6, DamageDice = "1d6",  DamageBonus = 3, DamageType = DamageType.Slashing,    Range = "5 ft"   },
            new() { Name = "Shortsword",      ActionType = ActionType.Attack, AttackBonus = 6, DamageDice = "1d6",  DamageBonus = 3, DamageType = DamageType.Piercing,    Range = "5 ft"   },
            new() { Name = "Longbow",         ActionType = ActionType.Attack, AttackBonus = 5, DamageDice = "1d8",  DamageBonus = 2, DamageType = DamageType.Piercing,    Range = "150 ft" },
            new() { Name = "Unarmed Strike",  ActionType = ActionType.Attack, AttackBonus = 6, DamageDice = "1",    DamageBonus = 3, DamageType = DamageType.Bludgeoning, Range = "5 ft"   },
        },

        Skills = new List<CharacterSkill>
        {
            new() { Skill = Skill.Acrobatics,     Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.AnimalHandling, Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Nature,         Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Survival,       Proficiency = ProficiencyLevel.Proficient },
        },

        Inventory = new List<InventoryItem>
        {
            Inv("Studded Leather", 1, ItemCategory.Armor, true),
            Inv("Handaxe", 1, ItemCategory.Weapon, true),
            Inv("Scimitar", 1, ItemCategory.Weapon, true),
            Inv("Shortsword", 1, ItemCategory.Weapon, true),
            Inv("Longbow", 1, ItemCategory.Weapon, true),
            Inv("Arrows", 20, ItemCategory.Other),
            Inv("Quiver", 1, ItemCategory.Other),
            Inv("Cartographer's Tools", 1, ItemCategory.Tool),
            Inv("Backpack", 1, ItemCategory.Other),
            Inv("Clothes, Common", 1, ItemCategory.Other),
            Inv("Iron Pot", 1, ItemCategory.Other),
            Inv("Shovel", 1, ItemCategory.Other),
            Inv("Rations", 10, ItemCategory.Consumable),
            Inv("Oil", 2, ItemCategory.Consumable),
            Inv("Torch", 10, ItemCategory.Other),
            Inv("Rope (50 ft)", 1, ItemCategory.Other),
            Inv("Waterskin", 1, ItemCategory.Other),
            Inv("Tinderbox", 1, ItemCategory.Other),
            Inv("Caltrops", 20, ItemCategory.Other),
            Inv("Crowbar", 1, ItemCategory.Other),
        },

        PersonalityTraits = "Boan Strickler — Fighter 5, Gnome Folk Hero. Extra Attack: 2 attacks per turn. Action Surge: 1/short rest. Gnome Cunning: Advantage on INT/WIS/CHA saves vs magic.",
    };

    public static Character Gideon => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000007"),
        Name           = "Gideon Silverspoon",
        Type           = CombatantType.PC,
        CharacterClass = "Bard",
        CharacterLevel = 5,
        Race           = "Half-Elf",
        Background     = "Criminal",

        Strength     = 10, Dexterity    = 15, Constitution = 13,
        Intelligence = 14, Wisdom       = 8,  Charisma     = 17,

        MaxHitPoints     = 38,
        ArmorClass       = 12,
        Speed            = 30,
        ProficiencyBonus = 3,

        SaveProfDexterity = true,
        SaveProfCharisma  = true,

        Actions = new List<CombatAction>
        {
            new() { Name = "Dagger",          ActionType = ActionType.Attack,      AttackBonus = 5, DamageDice = "1d4",  DamageBonus = 2, DamageType = DamageType.Piercing,  Range = "20 ft"  },
            new() { Name = "Vicious Mockery", ActionType = ActionType.Spell,       SaveDC = 14, SaveAbility = AbilityScore.Wisdom,        DamageDice = "2d4",  DamageType = DamageType.Psychic,  Range = "60 ft",  SpellLevel = 0 },
            new() { Name = "Shatter",         ActionType = ActionType.Spell,       SaveDC = 14, SaveAbility = AbilityScore.Constitution,  DamageDice = "3d8",  DamageType = DamageType.Thunder,  Range = "60 ft",  SpellLevel = 2, UsesPerDay = 3, UsesRemaining = 3 },
            new() { Name = "Hypnotic Pattern",ActionType = ActionType.Spell,       SaveDC = 14, SaveAbility = AbilityScore.Wisdom,        DamageDice = "",     DamageType = DamageType.None,     Range = "120 ft", SpellLevel = 3, UsesPerDay = 2, UsesRemaining = 2 },
        },

        SpellSlots = new List<SpellSlot>
        {
            new() { Level = 1, MaxSlots = 4, UsedSlots = 0 },
            new() { Level = 2, MaxSlots = 3, UsedSlots = 0 },
            new() { Level = 3, MaxSlots = 2, UsedSlots = 0 },
        },

        Skills = new List<CharacterSkill>
        {
            new() { Skill = Skill.Acrobatics,  Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Deception,   Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.History,     Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Perception,  Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Performance, Proficiency = ProficiencyLevel.Expertise  },
            new() { Skill = Skill.Persuasion,  Proficiency = ProficiencyLevel.Expertise  },
            new() { Skill = Skill.Stealth,     Proficiency = ProficiencyLevel.Proficient },
        },

        Spells = SpellsFor(
            "Vicious Mockery", "Mage Hand",
            "Healing Word", "Faerie Fire", "Charm Person",
            "Shatter", "Invisibility",
            "Hypnotic Pattern"),

        Inventory = new List<InventoryItem>
        {
            Inv("Leather Armor", 1, ItemCategory.Armor, true),
            Inv("Dagger", 4, ItemCategory.Weapon, true),
            Inv("Lute", 1, ItemCategory.Other, true),
            Inv("Thieves' Tools", 1, ItemCategory.Tool),
            Inv("Backpack", 1, ItemCategory.Other),
            Inv("Pouch", 2, ItemCategory.Other),
            Inv("Crowbar", 1, ItemCategory.Other),
            Inv("Traveler's Clothes", 1, ItemCategory.Other),
            Inv("Costume", 3, ItemCategory.Other),
            Inv("Rations", 9, ItemCategory.Consumable),
            Inv("Oil", 8, ItemCategory.Consumable),
            Inv("Bullseye Lantern", 1, ItemCategory.Other),
            Inv("Bedroll", 1, ItemCategory.Other),
            Inv("Blanket", 1, ItemCategory.Other),
            Inv("Mirror", 1, ItemCategory.Other),
            Inv("Waterskin", 2, ItemCategory.Other),
            Inv("Tinderbox", 2, ItemCategory.Other),
        },

        PersonalityTraits = "Gideon Silverspoon — Bard 5, Half-Elf Criminal. Bardic Inspiration d8, 3/short rest. College of Lore: Cutting Words reaction.",
    };

    public static Character Job => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000008"),
        Name           = "Job Goodhammer",
        Type           = CombatantType.PC,
        CharacterClass = "Paladin",
        CharacterLevel = 5,
        Race           = "Human",
        Background     = "Acolyte",

        Strength     = 14, Dexterity    = 13, Constitution = 15,
        Intelligence = 12, Wisdom       = 10, Charisma     = 12,

        MaxHitPoints     = 44,
        ArmorClass       = 11,
        Speed            = 30,
        ProficiencyBonus = 3,

        SaveProfWisdom   = true,
        SaveProfCharisma = true,

        Actions = new List<CombatAction>
        {
            new() { Name = "Longsword",       ActionType = ActionType.Attack, AttackBonus = 5, DamageDice = "1d8",  DamageBonus = 2, DamageType = DamageType.Slashing, Range = "5 ft"  },
            new() { Name = "Divine Smite",    ActionType = ActionType.Spell,  AttackBonus = 0, DamageDice = "2d8",  DamageBonus = 0, DamageType = DamageType.Radiant,  Range = "Self", SpellLevel = 1, UsesPerDay = 4, UsesRemaining = 4 },
            new() { Name = "Sacred Flame",    ActionType = ActionType.Spell,  SaveDC = 12, SaveAbility = AbilityScore.Dexterity, DamageDice = "2d8", DamageType = DamageType.Radiant, Range = "60 ft", SpellLevel = 0 },
            new() { Name = "Shield of Faith", ActionType = ActionType.Spell,  SaveDC = 0,  DamageDice = "", DamageType = DamageType.None, Range = "60 ft", SpellLevel = 1, UsesPerDay = 4, UsesRemaining = 4 },
        },

        SpellSlots = new List<SpellSlot>
        {
            new() { Level = 1, MaxSlots = 4, UsedSlots = 0 },
            new() { Level = 2, MaxSlots = 2, UsedSlots = 0 },
        },

        Skills = new List<CharacterSkill>
        {
            new() { Skill = Skill.Insight,      Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Intimidation, Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Religion,     Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Survival,     Proficiency = ProficiencyLevel.Proficient },
        },

        Spells = SpellsFor(
            "Spare the Dying", "Sacred Flame",
            "Shield of Faith",
            "Find Steed"),

        Inventory = new List<InventoryItem>
        {
            Inv("Chain Mail", 1, ItemCategory.Armor, true),
            Inv("Shield", 1, ItemCategory.Armor, true),
            Inv("Longsword", 1, ItemCategory.Weapon, true),
            Inv("Javelin", 6, ItemCategory.Weapon, true),
            Inv("Amulet (Holy Symbol)", 1, ItemCategory.Other, true),
            Inv("Calligrapher's Supplies", 1, ItemCategory.Tool),
            Inv("Backpack", 1, ItemCategory.Other),
            Inv("Book of Prayers", 1, ItemCategory.Other),
            Inv("Robe", 2, ItemCategory.Other),
            Inv("Rations", 7, ItemCategory.Consumable),
            Inv("Holy Water", 1, ItemCategory.Consumable),
            Inv("Blanket", 1, ItemCategory.Other),
            Inv("Lamp", 1, ItemCategory.Other),
            Inv("Tinderbox", 1, ItemCategory.Other),
        },

        PersonalityTraits = "Job Goodhammer — Paladin 5, Human Acolyte. Lay on Hands pool: 25 HP. Oath of the Open Sea.",
    };

    public static Character Bren => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000009"),
        Name           = "Bren Gunning",
        Type           = CombatantType.PC,
        CharacterClass = "Druid",
        CharacterLevel = 5,
        Race           = "Forest Gnome",
        Background     = "Custom",
        Subclass       = "Circle of the Land",

        Strength     = 11, Dexterity    = 13, Constitution = 9,
        Intelligence = 14, Wisdom       = 15, Charisma     = 13,

        MaxHitPoints     = 23,
        ArmorClass       = 12,
        Speed            = 30,
        ProficiencyBonus = 3,

        SaveProfIntelligence = true,
        SaveProfWisdom       = true,

        Actions = new List<CombatAction>
        {
            new() { Name = "Poison Spray",   ActionType = ActionType.SpellAttack, AttackBonus = 5, DamageDice = "2d12", DamageBonus = 0, DamageType = DamageType.Poison,    Range = "30 ft",  SpellLevel = 0 },
            new() { Name = "Shocking Grasp", ActionType = ActionType.SpellAttack, AttackBonus = 5, DamageDice = "2d8",  DamageBonus = 0, DamageType = DamageType.Lightning, Range = "Touch",  SpellLevel = 0 },
            new() { Name = "Dagger",         ActionType = ActionType.Attack,      AttackBonus = 4, DamageDice = "1d4",  DamageBonus = 1, DamageType = DamageType.Piercing,  Range = "20 ft" },
            new() { Name = "Thunderwave",    ActionType = ActionType.Spell,       SaveDC = 13, SaveAbility = AbilityScore.Constitution, DamageDice = "2d8",  DamageType = DamageType.Thunder,   Range = "15 ft Cube",     SpellLevel = 1, UsesPerDay = 4, UsesRemaining = 4 },
            new() { Name = "Healing Word",   ActionType = ActionType.Spell,       SaveDC = 0,  DamageDice = "2d4",  DamageType = DamageType.None,    Range = "60 ft",          SpellLevel = 1, UsesPerDay = 4, UsesRemaining = 4 },
            new() { Name = "Moonbeam",       ActionType = ActionType.Spell,       SaveDC = 13, SaveAbility = AbilityScore.Constitution, DamageDice = "2d10", DamageType = DamageType.Radiant,   Range = "120 ft/5 ft Cyl", SpellLevel = 2, UsesPerDay = 3, UsesRemaining = 3 },
            new() { Name = "Call Lightning", ActionType = ActionType.Spell,       SaveDC = 13, SaveAbility = AbilityScore.Dexterity,    DamageDice = "3d10", DamageType = DamageType.Lightning, Range = "120 ft/60 ft Cyl", SpellLevel = 3, UsesPerDay = 2, UsesRemaining = 2 },
        },

        SpellSlots = new List<SpellSlot>
        {
            new() { Level = 1, MaxSlots = 4, UsedSlots = 0 },
            new() { Level = 2, MaxSlots = 3, UsedSlots = 0 },
            new() { Level = 3, MaxSlots = 2, UsedSlots = 0 },
        },

        Skills = new List<CharacterSkill>
        {
            new() { Skill = Skill.Nature,       Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Perception,   Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.SleightOfHand,Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Stealth,      Proficiency = ProficiencyLevel.Proficient },
        },

        // Druids prepare from the whole list; Bren's PDF lists nearly all druid
        // spells, so this is a sensible prepared subset (his cantrips are exact).
        Spells = SpellsFor(
            "Spare the Dying", "Poison Spray", "Minor Illusion", "Shocking Grasp", "Elementalism",
            "Thunderwave", "Healing Word", "Faerie Fire", "Cure Wounds", "Fog Cloud",
            "Moonbeam", "Flaming Sphere", "Heat Metal",
            "Call Lightning"),

        Inventory = new List<InventoryItem>
        {
            Inv("Leather Armor", 1, ItemCategory.Armor, true),
            Inv("Dagger", 1, ItemCategory.Weapon, true),
            Inv("Shortsword", 1, ItemCategory.Weapon, true),
            Inv("Wooden Staff (Druidic Focus)", 1, ItemCategory.Other, true),
        },

        PersonalityTraits = "Bren Gunning — Druid 5, Forest Gnome. Circle of the Land. Wild Shape 2/long rest (5 temp HP). Gnome Cunning: Advantage on INT/WIS/CHA saves vs magic.",
    };
}
