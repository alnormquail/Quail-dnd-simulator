using CopilotTest.Models;

namespace CopilotTest.Services;

// RestKind and ResourcePool now live in Models/CombatResources.cs so the
// Combatant model can carry pools for live, shared combat.

/// <summary>Live, in-session play state for one character (current HP, slots used, pools).</summary>
public class LiveResources
{
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int TempHp { get; set; }

    public Dictionary<int, int> SlotsUsed { get; } = new();   // slot level -> used
    public Dictionary<Guid, int> ActionUses { get; } = new(); // action id -> uses remaining
    public List<ResourcePool> Pools { get; } = new();
}

/// <summary>
/// Tracks live play state for the current browser session (Blazor circuit-scoped).
/// Initialized from the saved character template; reset by Short/Long rests.
/// Not persisted to the database yet — that's a Phase 1 upgrade.
/// </summary>
public class PlayState
{
    private readonly Dictionary<Guid, LiveResources> _state = new();

    public LiveResources For(Character c)
    {
        if (_state.TryGetValue(c.Id, out var existing)) return existing;
        var live = Build(c);
        _state[c.Id] = live;
        return live;
    }

    public void Reset(Character c) => _state[c.Id] = Build(c);

    public void Rest(Character c, RestKind kind)
    {
        var live = For(c);

        if (kind == RestKind.Long)
        {
            live.CurrentHp = live.MaxHp;
            live.TempHp = 0;
            foreach (var key in live.SlotsUsed.Keys.ToList()) live.SlotsUsed[key] = 0;
            foreach (var a in c.Actions) live.ActionUses[a.Id] = a.UsesPerDay;
            foreach (var p in live.Pools) p.Current = p.Max;
        }
        else // short rest: only short-rest resources return (2014 rules)
        {
            foreach (var p in live.Pools.Where(p => p.RestoreOn == RestKind.Short))
                p.Current = p.Max;
        }
    }

    private static LiveResources Build(Character c)
    {
        var live = new LiveResources
        {
            CurrentHp = c.MaxHitPoints,
            MaxHp = c.MaxHitPoints,
            TempHp = 0,
        };
        foreach (var s in c.SpellSlots) live.SlotsUsed[s.Level] = s.UsedSlots;
        foreach (var a in c.Actions) live.ActionUses[a.Id] = a.UsesPerDay;
        live.Pools.AddRange(BuildPools(c));
        return live;
    }

    /// <summary>Class-specific resource pools (2014 rules) for the party's classes.</summary>
    private static IEnumerable<ResourcePool> BuildPools(Character c)
    {
        var cls = (c.CharacterClass ?? "").Trim();
        var lvl = c.CharacterLevel;

        if (c.IsBarbarianClass || cls.Equals("Barbarian", StringComparison.OrdinalIgnoreCase))
        {
            var rages = c.RageUsesPerDay > 0 ? c.RageUsesPerDay : 3;
            yield return new ResourcePool { Name = "Rage", Current = rages, Max = rages, RestoreOn = RestKind.Long, Note = $"+{c.RageBonus} dmg" };
        }

        if (cls.Equals("Paladin", StringComparison.OrdinalIgnoreCase))
        {
            var loh = lvl * 5;
            yield return new ResourcePool { Name = "Lay on Hands", Current = loh, Max = loh, RestoreOn = RestKind.Long, Note = "HP pool" };
            yield return new ResourcePool { Name = "Channel Divinity", Current = 1, Max = 1, RestoreOn = RestKind.Short };
        }

        if (cls.Equals("Bard", StringComparison.OrdinalIgnoreCase))
        {
            var uses = Math.Max(1, c.CharismaModifier);
            var die = lvl >= 15 ? "d12" : lvl >= 10 ? "d10" : lvl >= 5 ? "d8" : "d6";
            var restoreOn = lvl >= 5 ? RestKind.Short : RestKind.Long; // Font of Inspiration at 5
            yield return new ResourcePool { Name = "Bardic Inspiration", Current = uses, Max = uses, RestoreOn = restoreOn, Note = die };
        }

        if (cls.Equals("Fighter", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ResourcePool { Name = "Action Surge", Current = 1, Max = 1, RestoreOn = RestKind.Short };
            yield return new ResourcePool { Name = "Second Wind", Current = 1, Max = 1, RestoreOn = RestKind.Short, Note = "1d10+lvl HP" };
        }
    }
}
