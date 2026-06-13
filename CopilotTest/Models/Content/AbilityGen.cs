namespace CopilotTest.Models.Content;

/// <summary>Helpers for 2024 ability-score generation methods.</summary>
public static class AbilityGen
{
    /// <summary>Standard Array values to assign across the six abilities.</summary>
    public static readonly int[] StandardArray = [15, 14, 13, 12, 10, 8];

    public const int PointBuyBudget = 27;

    /// <summary>Point Buy cost for a given score (8–15). Returns -1 if out of range.</summary>
    public static int PointBuyCost(int score) => score switch
    {
        8 => 0, 9 => 1, 10 => 2, 11 => 3, 12 => 4, 13 => 5, 14 => 7, 15 => 9,
        _ => -1,
    };

    /// <summary>Total points spent for a set of scores under Point Buy.</summary>
    public static int PointBuySpent(IEnumerable<int> scores) =>
        scores.Sum(s => Math.Max(0, PointBuyCost(s)));
}
