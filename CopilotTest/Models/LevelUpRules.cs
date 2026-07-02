namespace CopilotTest.Models;

/// <summary>
/// Static 5e rules data used by the level-up wizard: hit dice, proficiency
/// progression, spell slot tables, and per-class feature hints.
/// </summary>
public static class LevelUpRules
{
    // ── Hit dice ─────────────────────────────────────────────────────────────

    public static int GetHitDie(string characterClass) => characterClass.Trim().ToLowerInvariant() switch
    {
        "barbarian" => 12,
        "fighter" or "paladin" or "ranger" => 10,
        "sorcerer" or "wizard" => 6,
        _ => 8, // Bard, Cleric, Druid, Monk, Rogue, Warlock, Artificer, unknown
    };

    /// <summary>Average HP gain on level up: half the hit die + 1, plus CON modifier (minimum 1).</summary>
    public static int AverageHpGain(string characterClass, int conModifier) =>
        Math.Max(1, GetHitDie(characterClass) / 2 + 1 + conModifier);

    // ── Proficiency bonus ────────────────────────────────────────────────────

    public static int ProficiencyForLevel(int level) => 2 + (Math.Clamp(level, 1, 20) - 1) / 4;

    // ── Ability Score Improvements ───────────────────────────────────────────

    public static bool IsAsiLevel(string characterClass, int newLevel)
    {
        var cls = characterClass.Trim().ToLowerInvariant();
        if (cls == "fighter" && (newLevel == 6 || newLevel == 14)) return true;
        if (cls == "rogue" && newLevel == 10) return true;
        return newLevel is 4 or 8 or 12 or 16 or 19;
    }

    // ── Spell slots ──────────────────────────────────────────────────────────

    public enum CasterType { None, Full, Half }

    public static CasterType GetCasterType(string characterClass) => characterClass.Trim().ToLowerInvariant() switch
    {
        "bard" or "cleric" or "druid" or "sorcerer" or "wizard" => CasterType.Full,
        "paladin" or "ranger" => CasterType.Half,
        _ => CasterType.None,
    };

    // Full caster slot table indexed by [level 1-20][slot level 1-9]
    private static readonly int[][] FullCasterSlots =
    [
        [2,0,0,0,0,0,0,0,0], [3,0,0,0,0,0,0,0,0], [4,2,0,0,0,0,0,0,0], [4,3,0,0,0,0,0,0,0],
        [4,3,2,0,0,0,0,0,0], [4,3,3,0,0,0,0,0,0], [4,3,3,1,0,0,0,0,0], [4,3,3,2,0,0,0,0,0],
        [4,3,3,3,1,0,0,0,0], [4,3,3,3,2,0,0,0,0], [4,3,3,3,2,1,0,0,0], [4,3,3,3,2,1,0,0,0],
        [4,3,3,3,2,1,1,0,0], [4,3,3,3,2,1,1,0,0], [4,3,3,3,2,1,1,1,0], [4,3,3,3,2,1,1,1,0],
        [4,3,3,3,2,1,1,1,1], [4,3,3,3,3,1,1,1,1], [4,3,3,3,3,2,1,1,1], [4,3,3,3,3,2,2,1,1],
    ];

    // Half caster slot table (Paladin/Ranger, PHB-2024 style: slots from level 1)
    private static readonly int[][] HalfCasterSlots =
    [
        [2,0,0,0,0], [2,0,0,0,0], [3,0,0,0,0], [3,0,0,0,0],
        [4,2,0,0,0], [4,2,0,0,0], [4,3,0,0,0], [4,3,0,0,0],
        [4,3,2,0,0], [4,3,2,0,0], [4,3,3,0,0], [4,3,3,0,0],
        [4,3,3,1,0], [4,3,3,1,0], [4,3,3,2,0], [4,3,3,2,0],
        [4,3,3,3,1], [4,3,3,3,1], [4,3,3,3,2], [4,3,3,3,2],
    ];

    /// <summary>Returns (slotLevel, maxSlots) pairs for a class at the given character level.</summary>
    public static List<(int Level, int Max)> SlotsForLevel(string characterClass, int level)
    {
        level = Math.Clamp(level, 1, 20);
        var table = GetCasterType(characterClass) switch
        {
            CasterType.Full => FullCasterSlots[level - 1],
            CasterType.Half => HalfCasterSlots[level - 1],
            _ => null,
        };
        if (table == null) return [];
        return table.Select((max, i) => (Level: i + 1, Max: max)).Where(t => t.Max > 0).ToList();
    }

    // ── Feature grants ───────────────────────────────────────────────────────

    public record FeatureGrant(string Name, string Description);

    /// <summary>
    /// Class features automatically added to the sheet at a level (base class only —
    /// subclass features vary, so they're surfaced as hints instead).
    /// Covers levels 1-10 for the 2024 PHB classes. The character builder replays
    /// levels 1..N when creating at level N, so built and leveled sheets match.
    /// </summary>
    public static FeatureGrant[] FeatureGrants(string characterClass, int newLevel)
    {
        var cls = characterClass.Trim().ToLowerInvariant();
        return (cls, newLevel) switch
        {
            // ── Level 1-4 base features (2024 PHB) ──
            ("barbarian", 1) => [new("Rage", "Bonus Action: enter a rage for bonus melee damage and resistance to bludgeoning/piercing/slashing damage."),
                                 new("Unarmored Defense", "Without armor, your AC is 10 + DEX mod + CON mod."),
                                 new("Weapon Mastery", "Use the mastery property of 2 weapon types you're proficient with.")],
            ("barbarian", 2) => [new("Reckless Attack", "Gain advantage on STR melee attacks this turn; attacks against you have advantage until your next turn."),
                                 new("Danger Sense", "Advantage on DEX saving throws unless incapacitated.")],
            ("barbarian", 3) => [new("Primal Knowledge", "Gain another skill; while raging, use STR for Acrobatics, Intimidation, Perception, Stealth, or Survival.")],
            ("bard", 1)      => [new("Bardic Inspiration", "Bonus Action: give a creature a d6 to add to a d20 test (CHA mod uses/long rest)."),
                                 new("Spellcasting", "Cast Bard spells using Charisma.")],
            ("bard", 2)      => [new("Expertise", "Double proficiency bonus in two skill proficiencies."),
                                 new("Jack of All Trades", "Add half proficiency to ability checks that don't already use it.")],
            ("cleric", 1)    => [new("Spellcasting", "Cast Cleric spells using Wisdom."),
                                 new("Divine Order", "Choose Protector (martial weapons + heavy armor) or Thaumaturge (extra cantrip, bonus to Arcana/Religion).")],
            ("cleric", 2)    => [new("Channel Divinity", "Divine Spark (deal/heal d8s) or Turn Undead (2 uses/short rest at this level).")],
            ("druid", 1)     => [new("Spellcasting", "Cast Druid spells using Wisdom."),
                                 new("Druidic", "You know the secret language of druids."),
                                 new("Primal Order", "Choose Magician (extra cantrip, bonus to Arcana/Nature) or Warden (martial weapons + medium armor).")],
            ("druid", 2)     => [new("Wild Shape", "Bonus Action: transform into a beast form (2 uses/long rest at this level)."),
                                 new("Wild Companion", "Expend a Wild Shape use to cast Find Familiar without components.")],
            ("fighter", 1)   => [new("Fighting Style", "Choose a Fighting Style feat (e.g. Defense, Great Weapon Fighting, Archery)."),
                                 new("Second Wind", "Bonus Action: regain 1d10 + Fighter level HP (2 uses, regain on short/long rest)."),
                                 new("Weapon Mastery", "Use the mastery property of 3 weapon types you're proficient with.")],
            ("fighter", 2)   => [new("Action Surge", "Take one additional action on your turn (1/short or long rest)."),
                                 new("Tactical Mind", "When you fail an ability check, expend a Second Wind use to add 1d10.")],
            ("monk", 1)      => [new("Martial Arts", "Unarmed strikes use a Martial Arts die (d6) and DEX; Bonus Action unarmed strike."),
                                 new("Unarmored Defense", "Without armor or shield, your AC is 10 + DEX mod + WIS mod.")],
            ("monk", 2)      => [new("Monk's Focus", "Focus Points fuel Flurry of Blows, Patient Defense, and Step of the Wind."),
                                 new("Unarmored Movement", "+10 ft speed while unarmored."),
                                 new("Uncanny Metabolism", "When you roll Initiative, regain all Focus Points (1/long rest).")],
            ("monk", 3)      => [new("Deflect Attacks", "Reaction: reduce damage from an attack by 1d10 + DEX mod + Monk level.")],
            ("monk", 4)      => [new("Slow Fall", "Reaction: reduce falling damage by 5 × Monk level.")],
            ("paladin", 1)   => [new("Lay On Hands", "Restore HP from a pool of 5 × Paladin level (Bonus Action)."),
                                 new("Spellcasting", "Cast Paladin spells using Charisma."),
                                 new("Weapon Mastery", "Use the mastery property of 2 weapon types you're proficient with.")],
            ("paladin", 2)   => [new("Fighting Style", "Choose a Fighting Style feat (or Blessed Warrior for cantrips)."),
                                 new("Paladin's Smite", "You always have Divine Smite prepared; cast it once per long rest without a slot.")],
            ("paladin", 3)   => [new("Channel Divinity", "Divine Sense plus your subclass's Channel Divinity option (2 uses/short rest at this level).")],
            ("ranger", 1)    => [new("Favored Enemy", "You always have Hunter's Mark prepared; cast it twice per long rest without a slot."),
                                 new("Spellcasting", "Cast Ranger spells using Wisdom."),
                                 new("Weapon Mastery", "Use the mastery property of 2 weapon types you're proficient with.")],
            ("ranger", 2)    => [new("Deft Explorer", "Expertise in one skill; learn two languages."),
                                 new("Fighting Style", "Choose a Fighting Style feat (or Druidic Warrior for cantrips).")],
            ("rogue", 1)     => [new("Sneak Attack", "Once per turn, +1d6 damage (scales with level) with advantage or an adjacent ally."),
                                 new("Expertise", "Double proficiency bonus in two skill proficiencies."),
                                 new("Thieves' Cant", "You know the secret language of rogues."),
                                 new("Weapon Mastery", "Use the mastery property of 2 weapon types you're proficient with.")],
            ("rogue", 2)     => [new("Cunning Action", "Bonus Action: Dash, Disengage, or Hide.")],
            ("rogue", 3)     => [new("Steady Aim", "Bonus Action: advantage on your next attack this turn if you don't move.")],
            ("sorcerer", 1)  => [new("Spellcasting", "Cast Sorcerer spells using Charisma."),
                                 new("Innate Sorcery", "Bonus Action: 1 minute of +1 spell save DC and advantage on spell attacks (2/long rest).")],
            ("sorcerer", 2)  => [new("Font of Magic", "Sorcery Points (= Sorcerer level) convert to/from spell slots."),
                                 new("Metamagic", "Learn 2 Metamagic options to modify your spells.")],
            ("warlock", 1)   => [new("Pact Magic", "Cast Warlock spells using Charisma; slots recharge on a short rest."),
                                 new("Eldritch Invocations", "Learn 1 invocation (e.g. Pact of the Blade, Agonizing Blast prerequisite chain).")],
            ("warlock", 2)   => [new("Magical Cunning", "1/long rest: regain half your Pact Magic slots after 1 minute of ritual.")],
            ("wizard", 1)    => [new("Spellcasting", "Cast Wizard spells using Intelligence; maintain a spellbook."),
                                 new("Arcane Recovery", "Once per day on a short rest, recover spell slots totaling half your Wizard level (rounded up)."),
                                 new("Ritual Adept", "Cast any ritual spell in your spellbook without preparing it.")],
            ("wizard", 2)    => [new("Scholar", "Expertise in one of Arcana, History, Investigation, Medicine, Nature, or Religion.")],

            // ── Barbarian ──
            ("barbarian", 5) => [new("Extra Attack", "Attack twice whenever you take the Attack action."),
                                 new("Fast Movement", "+10 ft speed while not wearing heavy armor.")],
            ("barbarian", 7) => [new("Feral Instinct", "Advantage on Initiative rolls."),
                                 new("Instinctive Pounce", "When you Rage, move up to half your speed for free.")],
            ("barbarian", 9) => [new("Brutal Strike", "When attacking with Reckless Attack, you can forgo advantage on one attack to deal +1d10 damage and push or slow the target.")],

            // ── Bard ──
            ("bard", 5)  => [new("Font of Inspiration", "Regain all Bardic Inspiration uses on a short or long rest.")],
            ("bard", 7)  => [new("Countercharm", "Reaction when you or an ally within 30 ft fails a save vs charm/fright: the save is rerolled with advantage.")],
            ("bard", 10) => [new("Magical Secrets", "Learn two spells from any class's spell list.")],

            // ── Druid ──
            ("druid", 5)  => [new("Wild Resurgence", "Once per turn, expend a spell slot to regain a Wild Shape use; or 1/long rest expend a Wild Shape use to gain a level 1 spell slot.")],
            ("druid", 7)  => [new("Elemental Fury (improvement)", "Subclass-dependent feature improvements; see PHB.")],
            ("druid", 9)  => [new("5th-level spells", "You can now prepare and cast 5th-level Druid spells.")],

            // ── Fighter ──
            ("fighter", 5) => [new("Extra Attack", "Attack twice whenever you take the Attack action."),
                               new("Tactical Shift", "When you use Second Wind, also move up to half your speed without provoking opportunity attacks.")],
            ("fighter", 9) => [new("Indomitable", "Reroll a failed saving throw, adding your Fighter level (1/long rest)."),
                               new("Tactical Master", "Swap a weapon's mastery property for Push, Sap, or Slow.")],
            ("fighter", 10) => [new("Subclass feature (level 10)", "Your subclass grants a feature at this level — see PHB.")],

            // ── Paladin ──
            ("paladin", 6)  => [new("Aura of Protection", "You and allies within 10 ft add your CHA modifier to all saving throws while you're conscious.")],
            ("paladin", 9)  => [new("Abjure Foes (Channel Divinity)", "Frighten foes within 60 ft: CHA save or frightened for 1 minute, only able to move, act, or use a bonus action each turn.")],
            ("paladin", 10) => [new("Aura of Courage", "You and allies in your aura can't be frightened.")],

            // ── Rogue ──
            ("rogue", 5) => [new("Cunning Strike", "Trade Sneak Attack dice for effects: 1d6 for Poison/Trip/Withdraw."),
                             new("Uncanny Dodge", "Reaction when hit by an attack you can see: halve its damage.")],
            ("rogue", 7) => [new("Evasion", "On DEX saves for half damage: take none on success, half on failure."),
                             new("Reliable Talent", "Treat d20 rolls of 9 or lower as 10 on proficient ability checks.")],
            ("rogue", 9) => [new("Subclass feature (level 9)", "Your subclass grants a feature at this level — see PHB.")],

            // ── Sorcerer ──
            ("sorcerer", 5) => [new("Sorcerous Restoration", "Once per long rest, regain half your Sorcerer level in Sorcery Points on a short rest.")],
            ("sorcerer", 7) => [new("Sorcery Incarnate", "When you have no Sorcery Points, regain 2 by using Innate Sorcery; you can also use two Metamagic options on one spell while Innate Sorcery is active.")],
            ("sorcerer", 9) => [new("5th-level spells", "You can now learn and cast 5th-level Sorcerer spells.")],

            // ── Wizard ──
            ("wizard", 5) => [new("Memorize Spell", "Swap one prepared spell after a short rest.")],
            ("wizard", 9) => [new("5th-level spells", "You can now prepare and cast 5th-level Wizard spells.")],

            // ── Cleric ──
            ("cleric", 5) => [new("Sear Undead", "Destroy Undead: on Turn Undead, deal 1d8 × WIS mod radiant damage to undead that fail.")],
            ("cleric", 7) => [new("Blessed Strikes", "Add 1d8 radiant damage to a cantrip or weapon strike once per turn.")],
            ("cleric", 10) => [new("Divine Intervention", "Once per long rest, cast any Cleric spell of 5th level or lower without a slot.")],

            _ => [],
        };
    }

    // ── Feature hints ────────────────────────────────────────────────────────

    /// <summary>Short reminders of what each class gains at a level (levels 2-12, common classes).</summary>
    public static string[] FeatureHints(string characterClass, int newLevel)
    {
        var cls = characterClass.Trim().ToLowerInvariant();
        var hints = (cls, newLevel) switch
        {
            ("barbarian", 6) => new[] { "Subclass feature (e.g. Aspect of the Beast)", "Rage damage +2, 4 rages/day" },
            ("barbarian", 7) => new[] { "Feral Instinct: advantage on Initiative", "Instinctive Pounce" },
            ("barbarian", 9) => new[] { "Brutal Strike: forgo advantage for +1d10 and an effect" },
            ("bard", 6) => new[] { "Subclass feature (Lore: Magical Discoveries)", "New spell known" },
            ("bard", 7) => new[] { "Countercharm: reaction to protect vs charm/fright" },
            ("druid", 6) => new[] { "Subclass feature (Land: Natural Recovery — regain slots on short rest)" },
            ("druid", 7) => new[] { "Elemental Fury improves at later levels; new 4th-level spells at L7" },
            ("fighter", 6) => new[] { "Ability Score Improvement (bonus Fighter ASI!)" },
            ("fighter", 7) => new[] { "Subclass feature", "Remember Action Surge + Second Wind on short rests" },
            ("paladin", 6) => new[] { "Aura of Protection: allies within 10 ft add your CHA mod to saves" },
            ("paladin", 7) => new[] { "Subclass aura (Devotion: Aura of Devotion — immunity to charm in aura)" },
            ("rogue", 5) => new[] { "Uncanny Dodge: reaction to halve an attack's damage", "Sneak Attack 3d6" },
            ("rogue", 6) => new[] { "Expertise: pick two more skills to double proficiency", },
            ("rogue", 7) => new[] { "Evasion: DEX save for half → no damage on success", "Sneak Attack 4d6" },
            ("sorcerer", 6) => new[] { "Subclass feature (Wild Magic: Bend Luck — spend 1 SP to add/subtract 1d4 from a roll)", "6 Sorcery Points" },
            ("sorcerer", 7) => new[] { "Sorcery Incarnate at later levels; new 4th-level spells at L7", "7 Sorcery Points" },
            ("wizard", 6) => new[] { "Subclass feature", "Learn 2 new spells" },
            _ => Array.Empty<string>(),
        };

        if (hints.Length > 0) return hints;

        // Generic fallbacks
        return newLevel switch
        {
            5 when GetCasterType(characterClass) == CasterType.Full =>
                ["3rd-level spells unlocked!", "Check your class table for new features"],
            5 => ["Extra Attack (most martial classes)", "Check your class table for new features"],
            _ => ["Check your class/subclass table for features gained at this level",
                  "Casters: you may learn or prepare new spells"],
        };
    }
}
