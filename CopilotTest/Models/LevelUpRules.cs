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
