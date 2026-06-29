namespace CopilotTest.Models;

/// <summary>
/// Hard-coded character templates for the party members.
/// Used to seed the database on first run.
/// </summary>
public static class PreloadedCharacters
{
    public static IReadOnlyList<Character> All => [Winnie, Kennyth, Boan, Gideon, Job, Bren, Korran];

    /// <summary>Builds Spell entries from the shared library by name (skips unknowns).</summary>
    private static List<Spell> SpellsFor(params string[] names) =>
        names.Select(n => SpellLibrary.All.FirstOrDefault(s => s.Name == n))
             .Where(s => s != null)
             .Select(s => SpellLibrary.ToSpell(s!, Guid.Empty))
             .ToList();

    private static InventoryItem Inv(string name, int qty, ItemCategory cat, bool equipped = false) =>
        new() { Name = name, Quantity = qty, Category = cat, IsEquipped = equipped };

    private static CharacterFeature Feat(string name, string source, string desc) =>
        new() { Name = name, Source = source, Description = desc, LevelGained = 1 };

    public static Character Winnie => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000004"),
        Name           = "Winnie Vale",
        Type           = CombatantType.PC,
        CharacterClass = "Sorcerer",
        CharacterLevel = 5,
        Race           = "High Elf",
        Background     = "Noble",
        Subclass       = "Wild Magic Sorcery",
        SubclassKey    = "sorc-wildmagic",

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

        Features = new List<CharacterFeature>
        {
            Feat("Font of Magic (5 SP)", "Sorcerer", "Convert Sorcery Points into spell slots or vice versa; fuel Metamagic."),
            Feat("Metamagic", "Sorcerer", "Subtle Spell (cast with no V/S components) and Twinned Spell (target a second creature)."),
            Feat("Innate Sorcery", "Sorcerer", "Bonus Action, 2/long rest: +1 spell save DC and advantage on spell attacks for 1 minute."),
            Feat("Wild Magic Surge", "Wild Magic Sorcery", "Casting a leveled spell can trigger a chaotic d100 surge (at the DM's discretion)."),
            Feat("Tides of Chaos", "Wild Magic Sorcery", "1/long rest: gain Advantage on a d20 roll; recharges when you cause a surge."),
            Feat("Bend Luck", "Wild Magic Sorcery", "Reaction, 2 SP: add or subtract 1d4 from any creature's d20 roll."),
            Feat("Fey Ancestry & Trance", "High Elf", "Advantage vs Charmed; immune to magical sleep; 4-hour Trance. Darkvision 60 ft."),
        },

        PersonalityTraits = "Winnie Vale — Wild Magic Sorcerer 5, High Elf Noble.",
        GoldPieces = 50,
    };

    public static Character Kennyth => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000005"),
        Name           = "Kennyth",
        Subclass       = "Oath of Devotion",
        SubclassKey    = "pal-devotion",
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

        Features = new List<CharacterFeature>
        {
            Feat("Lay on Hands (25 HP pool)", "Paladin", "Bonus Action: heal from a 25 HP/long-rest pool, or spend 5 HP to cure poison."),
            Feat("Divine Smite", "Paladin", "Expend a spell slot on a melee hit: +2d8 radiant (1st) / +3d8 (2nd); +1d8 vs Undead/Fiends. Free 1/long rest."),
            Feat("Channel Divinity (2/long rest)", "Paladin", "Divine Sense, or Sacred Weapon."),
            Feat("Extra Attack", "Paladin", "Attack twice whenever you take the Attack action."),
            Feat("Sacred Weapon", "Oath of Devotion", "Channel Divinity: add CHA to weapon attacks and emit light for 1 minute."),
            Feat("Gnomish Cunning", "Gnome", "Advantage on INT, WIS, and CHA saving throws. Darkvision 60 ft."),
        },

        PersonalityTraits = "Kennyth — Paladin 5, Gnome. Lay on Hands pool: 25 HP. Gnome Cunning: Advantage on INT/WIS/CHA saves vs magic.",
        GoldPieces = 30,
    };

    public static Character Boan => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000006"),
        Name           = "Boan Strickler",
        Subclass       = "Champion",
        SubclassKey    = "fighter-champion",
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

        Features = new List<CharacterFeature>
        {
            Feat("Fighting Style: Two-Weapon Fighting", "Fighter", "Add your ability modifier (STR +3) to the damage of your off-hand attack."),
            Feat("Weapon Masteries", "Fighter", "Scimitar (Nick): make the two-weapon off-hand attack as part of your Attack action. Shortsword & Handaxe (Vex): a hit gives Advantage on your next attack vs that target."),
            Feat("Extra Attack", "Fighter", "Attack twice whenever you take the Attack action (3 swings with the Scimitar's Nick)."),
            Feat("Action Surge (1/short rest)", "Fighter", "Take one additional action on your turn."),
            Feat("Second Wind (3/long rest)", "Fighter", "Bonus Action: heal 1d10+5; also fuels Tactical Mind on a failed check."),
            Feat("Improved Critical", "Champion", "Score a critical hit on a 19-20."),
            Feat("Gnomish Cunning", "Gnome", "Advantage on INT, WIS, and CHA saving throws. Darkvision 60 ft."),
        },

        PersonalityTraits = "Boan Strickler — Fighter 5, Gnome Folk Hero. Extra Attack: 2 attacks per turn. Action Surge: 1/short rest. Gnome Cunning: Advantage on INT/WIS/CHA saves vs magic.",
    };

    public static Character Gideon => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000007"),
        Name           = "Gideon Silverspoon",
        Subclass       = "College of Lore",
        SubclassKey    = "bard-lore",
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

        Features = new List<CharacterFeature>
        {
            Feat("Bardic Inspiration (d6, 3/short rest)", "Bard", "Bonus Action: give an ally a d6 to add to a roll within 10 minutes."),
            Feat("Expertise", "Bard", "Double proficiency on Performance and Persuasion (+9)."),
            Feat("Jack of All Trades", "Bard", "Add half proficiency (+1) to ability checks you're not proficient in."),
            Feat("Font of Inspiration", "Bard", "Regain all Bardic Inspiration on a short or long rest."),
            Feat("Cutting Words", "College of Lore", "Reaction: expend Bardic Inspiration to subtract it from an enemy's attack, check, or damage."),
            Feat("Fey Ancestry", "Half-Elf", "Advantage vs Charmed; immune to magical sleep. Darkvision 60 ft."),
        },

        PersonalityTraits = "Gideon Silverspoon — Bard 5, Half-Elf Criminal. Bardic Inspiration d8, 3/short rest. College of Lore: Cutting Words reaction.",
    };

    public static Character Job => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-000000000008"),
        Name           = "Job Goodhammer",
        Subclass       = "Oath of the Open Sea",
        SubclassKey    = "pal-opensea",
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

        Features = new List<CharacterFeature>
        {
            Feat("Lay on Hands (25 HP pool)", "Paladin", "Bonus Action: heal from a 25 HP/long-rest pool, or spend 5 HP to cure poison."),
            Feat("Divine Smite", "Paladin", "Expend a spell slot on a melee hit: +2d8 radiant (1st) / +3d8 (2nd). Free 1/long rest."),
            Feat("Channel Divinity (2/long rest)", "Paladin", "Divine Sense, or Fury of the Tides (on a hit, push the target 10 ft for bonus damage)."),
            Feat("Extra Attack", "Paladin", "Attack twice whenever you take the Attack action."),
            Feat("Human Versatility", "Human", "Heroic Inspiration on a long rest, a bonus skill, and an Origin feat."),
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
        SubclassKey    = "druid-land",

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

        Features = new List<CharacterFeature>
        {
            Feat("Wild Shape (2/long rest)", "Druid", "Bonus Action: transform into a Beast for 2 hours, gaining 5 temp HP."),
            Feat("Druidic", "Druid", "You know Druidic and always have Speak with Animals prepared."),
            Feat("Primal Order: Magician", "Druid", "Know an extra cantrip; +2 bonus to INT (Arcana/Nature) checks."),
            Feat("Wild Resurgence & Wild Companion", "Druid", "Convert spell slots ↔ Wild Shape uses; expend a use to cast Find Familiar."),
            Feat("Land's Aid & Natural Recovery", "Circle of the Land", "Expend Wild Shape for a 10-ft AoE (DC 13 CON, 2d6 necrotic; heal an ally 2d6); recover spell slots on a short rest."),
            Feat("Gnomish Cunning", "Forest Gnome", "Advantage on INT, WIS, and CHA saving throws. Darkvision 60 ft."),
        },

        PersonalityTraits = "Bren Gunning — Druid 5, Forest Gnome. Circle of the Land. Wild Shape 2/long rest (5 temp HP). Gnome Cunning: Advantage on INT/WIS/CHA saves vs magic.",
    };

    public static Character Korran => new()
    {
        Id             = new Guid("a1000000-0000-0000-0000-00000000000a"),
        Name           = "Korran Vale",
        Type           = CombatantType.PC,
        CharacterClass = "Barbarian",
        CharacterLevel = 4,
        Race           = "Half-Orc",
        Background     = "Custom",

        Strength     = 17, Dexterity    = 13, Constitution = 15,
        Intelligence = 12, Wisdom       = 10, Charisma     = 8,

        MaxHitPoints     = 41,
        ArmorClass       = 17,
        Speed            = 30,
        ProficiencyBonus = 2,

        SaveProfStrength     = true,
        SaveProfConstitution = true,

        IsBarbarianClass = true,
        RageBonus        = 2,
        RageUsesPerDay   = 3,

        Actions = new List<CombatAction>
        {
            new() { Name = "Greataxe",       ActionType = ActionType.Attack, AttackBonus = 5, DamageDice = "1d12", DamageBonus = 3, DamageType = DamageType.Slashing,    Range = "5 ft"  },
            new() { Name = "Handaxe",        ActionType = ActionType.Attack, AttackBonus = 5, DamageDice = "1d6",  DamageBonus = 3, DamageType = DamageType.Slashing,    Range = "20/60 ft" },
            new() { Name = "Unarmed Strike", ActionType = ActionType.Attack, AttackBonus = 5, DamageDice = "1",    DamageBonus = 3, DamageType = DamageType.Bludgeoning, Range = "5 ft"  },
        },

        Skills = new List<CharacterSkill>
        {
            new() { Skill = Skill.Athletics,    Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Intimidation, Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Perception,   Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Performance,  Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Persuasion,   Proficiency = ProficiencyLevel.Proficient },
            new() { Skill = Skill.Survival,     Proficiency = ProficiencyLevel.Proficient },
        },

        Inventory = new List<InventoryItem>
        {
            Inv("Scale Mail (Armor of Force Resistance)", 1, ItemCategory.Armor, true),
            Inv("Shield", 1, ItemCategory.Armor, true),
            Inv("Greataxe", 2, ItemCategory.Weapon, true),
            Inv("Handaxe", 8, ItemCategory.Weapon, true),
            Inv("Backpack", 2, ItemCategory.Other),
            Inv("Rations", 10, ItemCategory.Consumable),
            Inv("Oil", 2, ItemCategory.Consumable),
            Inv("Torch", 10, ItemCategory.Other),
            Inv("Rope (50 ft)", 1, ItemCategory.Other),
            Inv("Bedroll", 1, ItemCategory.Other),
            Inv("Waterskin", 1, ItemCategory.Other),
            Inv("Tinderbox", 1, ItemCategory.Other),
        },

        Features = new List<CharacterFeature>
        {
            Feat("Rage (3/long rest)", "Barbarian", "Bonus Action: +2 melee damage, resistance to bludgeoning/piercing/slashing, advantage on STR checks & saves."),
            Feat("Reckless Attack", "Barbarian", "Attack with Advantage on your turn; attacks against you have Advantage until your next turn."),
            Feat("Danger Sense", "Barbarian", "Advantage on DEX saving throws against effects you can see."),
            Feat("Relentless Endurance", "Half-Orc", "When dropped to 0 HP (but not killed), drop to 1 HP instead — 1/long rest."),
            Feat("Savage Attacks", "Half-Orc", "On a melee critical hit, roll one of the weapon's damage dice an extra time."),
            Feat("Darkvision 60 ft.", "Half-Orc", "See in dim light within 60 ft as if bright light."),
        },

        PersonalityTraits = "Korran Vale — Barbarian 4, Half-Orc. Rage 3/long rest (+2 dmg, resistance to bludgeoning/piercing/slashing). Relentless Endurance: drop to 1 HP instead of 0 once per long rest.",
        GoldPieces = 30,
    };
}
