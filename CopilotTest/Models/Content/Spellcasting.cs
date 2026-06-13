namespace CopilotTest.Models.Content;

/// <summary>
/// 2024 PHB spellcasting allotments: how many cantrips a class knows and how
/// many spells it can prepare at each level. "By the book" for the core PHB
/// classes; non-casters return zero.
/// </summary>
public static class Spellcasting
{
    public record Limits(int Cantrips, int Prepared, bool IsCaster);

    public static bool IsCaster(string characterClass) => Key(characterClass) is not "";

    private static string Key(string c) => c.Trim().ToLowerInvariant() switch
    {
        "bard" => "bard", "cleric" => "cleric", "druid" => "druid", "paladin" => "paladin",
        "ranger" => "ranger", "sorcerer" => "sorcerer", "warlock" => "warlock", "wizard" => "wizard",
        _ => "",
    };

    // Prepared-spells counts by character level (index 0 = level 1), 2024 PHB tables.
    private static readonly Dictionary<string, int[]> Prepared = new()
    {
        ["bard"]     = [4,5,6,7,9,10,11,12,14,15,15,16,16,17,18,19,21,22,22,23],
        ["cleric"]   = [4,5,6,7,9,10,11,12,14,15,16,16,17,17,18,18,19,20,21,22],
        ["druid"]    = [4,5,6,7,9,10,11,12,14,15,16,16,17,17,18,18,19,20,21,22],
        ["paladin"]  = [2,3,4,5,6,6,7,7,9,9,10,10,11,11,12,12,14,14,15,15],
        ["ranger"]   = [2,3,4,5,6,6,7,7,9,9,10,10,11,11,12,12,14,14,15,15],
        ["sorcerer"] = [2,4,6,7,9,10,11,12,14,15,15,16,16,17,18,19,20,21,21,22],
        ["warlock"]  = [2,3,4,5,6,7,8,9,10,10,11,11,12,12,13,13,14,14,15,15],
        ["wizard"]   = [4,5,6,7,9,10,11,12,14,16,18,20,22,24,26,28,30,32,34,36],
    };

    // Cantrips known: (level 1-3, level 4-9, level 10+). Paladin/Ranger know none.
    private static readonly Dictionary<string, (int low, int mid, int high)> Cantrips = new()
    {
        ["bard"]     = (2, 3, 4),
        ["cleric"]   = (3, 4, 5),
        ["druid"]    = (2, 3, 4),
        ["sorcerer"] = (4, 5, 6),
        ["warlock"]  = (2, 3, 4),
        ["wizard"]   = (3, 4, 5),
        ["paladin"]  = (0, 0, 0),
        ["ranger"]   = (0, 0, 0),
    };

    public static Limits GetLimits(string characterClass, int level)
    {
        var key = Key(characterClass);
        if (key == "") return new Limits(0, 0, false);

        level = Math.Clamp(level, 1, 20);
        var prepared = Prepared.TryGetValue(key, out var arr) ? arr[level - 1] : 0;
        var ct = Cantrips.TryGetValue(key, out var c)
            ? (level >= 10 ? c.high : level >= 4 ? c.mid : c.low) : 0;

        return new Limits(ct, prepared, true);
    }
}
