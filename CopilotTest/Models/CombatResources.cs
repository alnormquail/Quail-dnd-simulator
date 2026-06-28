namespace CopilotTest.Models;

/// <summary>Whether a resource recharges on a short or long rest.</summary>
public enum RestKind { Short, Long }

/// <summary>A consumable resource pool (Lay on Hands, Bardic Inspiration, ...).</summary>
public class ResourcePool
{
    public string Name { get; set; } = "";
    public int Current { get; set; }
    public int Max { get; set; }
    public string? Note { get; set; }            // e.g. "d8", "HP pool"
    public RestKind RestoreOn { get; set; } = RestKind.Long;
    public bool UsePips => Max > 0 && Max <= 6;   // small pools show as clickable pips
}

/// <summary>One spell-slot tier's live state during an encounter.</summary>
public class SpellSlotState
{
    public int Level { get; set; }
    public int Max { get; set; }
    public int Used { get; set; }
    public int Remaining => Max - Used;
}

/// <summary>
/// Builds the live, in-combat resource pools for a combatant from its class and
/// level. Rage is tracked directly on <see cref="Combatant"/> (IsRaging /
/// RageUsesRemaining), so it is intentionally excluded here to avoid double
/// counting. Mirrors the per-class pools in <see cref="Services.PlayState"/>.
/// </summary>
public static class CombatResources
{
    public static List<ResourcePool> BuildPools(Combatant c)
    {
        var pools = new List<ResourcePool>();
        var cls = (c.CharacterClass ?? "").Trim();
        var lvl = c.CharacterLevel;

        if (cls.Equals("Paladin", StringComparison.OrdinalIgnoreCase))
        {
            var loh = lvl * 5;
            pools.Add(new ResourcePool { Name = "Lay on Hands", Current = loh, Max = loh, RestoreOn = RestKind.Long, Note = "HP pool" });
            pools.Add(new ResourcePool { Name = "Channel Divinity", Current = 1, Max = 1, RestoreOn = RestKind.Short });
        }

        if (cls.Equals("Bard", StringComparison.OrdinalIgnoreCase))
        {
            var uses = Math.Max(1, c.CharismaModifier);
            var die = lvl >= 15 ? "d12" : lvl >= 10 ? "d10" : lvl >= 5 ? "d8" : "d6";
            var restoreOn = lvl >= 5 ? RestKind.Short : RestKind.Long;   // Font of Inspiration at 5
            pools.Add(new ResourcePool { Name = "Bardic Inspiration", Current = uses, Max = uses, RestoreOn = restoreOn, Note = die });
        }

        if (cls.Equals("Fighter", StringComparison.OrdinalIgnoreCase))
        {
            pools.Add(new ResourcePool { Name = "Action Surge", Current = 1, Max = 1, RestoreOn = RestKind.Short });
            pools.Add(new ResourcePool { Name = "Second Wind", Current = 1, Max = 1, RestoreOn = RestKind.Short, Note = "1d10+lvl HP" });
        }

        return pools;
    }
}
