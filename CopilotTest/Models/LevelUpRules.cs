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
    /// Covers levels 2-10 for the classes in the party.
    /// </summary>
    public static FeatureGrant[] FeatureGrants(string characterClass, int newLevel)
    {
        var cls = characterClass.Trim().ToLowerInvariant();
        return (cls, newLevel) switch
        {
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
