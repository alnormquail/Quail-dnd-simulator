using System.Text.RegularExpressions;

namespace CopilotTest.Services;

public enum RollMode { Normal, Advantage, Disadvantage }

/// <summary>Result of a d20 check / attack / save roll.</summary>
public class D20Roll
{
    public IReadOnlyList<int> Rolls { get; init; } = Array.Empty<int>();
    public int Natural { get; init; }   // the kept d20 face
    public int Modifier { get; init; }
    public RollMode Mode { get; init; }

    public int Total => Natural + Modifier;
    public bool IsCrit => Natural == 20;
    public bool IsFumble => Natural == 1;

    public string Detail
    {
        get
        {
            var modText = Modifier == 0 ? "" : (Modifier > 0 ? $" + {Modifier}" : $" − {-Modifier}");
            if (Mode == RollMode.Normal)
                return $"d20 ({Natural}){modText} = {Total}";
            var tag = Mode == RollMode.Advantage ? "adv" : "dis";
            return $"d20 {tag} [{string.Join(", ", Rolls)}] → {Natural}{modText} = {Total}";
        }
    }
}

/// <summary>Result of a damage (or generic dice) roll.</summary>
public class DamageRoll
{
    public string Notation { get; init; } = "";
    public IReadOnlyList<int> Dice { get; init; } = Array.Empty<int>();
    public int Flat { get; init; }    // flat amount baked into the notation (e.g. "1")
    public int Bonus { get; init; }   // separate modifier
    public bool Crit { get; init; }

    public int Total => Dice.Sum() + Flat + Bonus;

    public string Detail
    {
        get
        {
            var parts = new List<string>();
            if (Dice.Count > 0) parts.Add($"[{string.Join(", ", Dice)}]");
            if (Flat != 0) parts.Add(Flat.ToString());
            if (Bonus != 0) parts.Add(Bonus > 0 ? $"+{Bonus}" : Bonus.ToString());
            var body = parts.Count > 0 ? string.Join(" ", parts) : "0";
            var critText = Crit ? " (crit — dice ×2)" : "";
            return $"{Notation}{critText}: {body} = {Total}";
        }
    }
}

/// <summary>Stateless dice roller. d20 rolls support advantage/disadvantage; damage parses "NdX" notation.</summary>
public class DiceService
{
    private static readonly Regex DicePattern = new(@"^\s*(\d*)\s*[dD]\s*(\d+)\s*$", RegexOptions.Compiled);

    private static int D(int sides) => Random.Shared.Next(1, sides + 1);

    public D20Roll RollD20(int modifier, RollMode mode = RollMode.Normal)
    {
        if (mode == RollMode.Normal)
        {
            var r = D(20);
            return new D20Roll { Rolls = new[] { r }, Natural = r, Modifier = modifier, Mode = mode };
        }

        var a = D(20);
        var b = D(20);
        var kept = mode == RollMode.Advantage ? Math.Max(a, b) : Math.Min(a, b);
        return new D20Roll { Rolls = new[] { a, b }, Natural = kept, Modifier = modifier, Mode = mode };
    }

    /// <summary>Rolls damage from a notation like "2d6", "1d12", "1", or "" plus a flat bonus. On a crit, dice count doubles.</summary>
    public DamageRoll RollDamage(string? diceNotation, int bonus = 0, bool crit = false)
    {
        var notation = (diceNotation ?? "").Trim();
        if (string.IsNullOrEmpty(notation))
            return new DamageRoll { Notation = bonus != 0 ? bonus.ToString() : "—", Bonus = bonus, Crit = crit };

        var m = DicePattern.Match(notation);
        if (m.Success)
        {
            var count = m.Groups[1].Value == "" ? 1 : int.Parse(m.Groups[1].Value);
            var sides = int.Parse(m.Groups[2].Value);
            if (crit) count *= 2;
            var rolls = new int[count];
            for (var i = 0; i < count; i++) rolls[i] = D(sides);
            return new DamageRoll { Notation = notation, Dice = rolls, Bonus = bonus, Crit = crit };
        }

        if (int.TryParse(notation, out var flat))
            return new DamageRoll { Notation = notation, Flat = flat, Bonus = bonus, Crit = crit };

        return new DamageRoll { Notation = notation, Bonus = bonus, Crit = crit };
    }
}
