using System.Text.RegularExpressions;

namespace CopilotTest.Models;

/// <summary>
/// Pure rules helpers that turn combatant state into mechanical outcomes — the
/// start of the "rules engine" layer. Today: deriving advantage/disadvantage on
/// an attack roll from the attacker's and target's conditions (2024 PHB), folded
/// together with the DM's manual choice. Any source of advantage plus any source
/// of disadvantage cancel to a normal roll.
/// </summary>
public static class CombatRules
{
    /// <summary>
    /// Net advantage state for <paramref name="attacker"/> attacking
    /// <paramref name="target"/> with <paramref name="action"/>, combined with the
    /// DM's <paramref name="manual"/> choice. Returns the mode and a short reason
    /// for the combat log.
    /// </summary>
    public static (AdvantageMode Mode, string Reason) ResolveAdvantage(
        Combatant attacker, Combatant target, CombatAction action, AdvantageMode manual)
    {
        bool melee = IsMelee(action);
        var adv = new List<string>();
        var dis = new List<string>();

        // Target's state gives the attacker advantage.
        if (target.Conditions.Contains(Condition.Blinded))    adv.Add("target Blinded");
        if (target.Conditions.Contains(Condition.Restrained)) adv.Add("target Restrained");
        if (target.Conditions.Contains(Condition.Stunned))    adv.Add("target Stunned");
        if (target.Conditions.Contains(Condition.Paralyzed))  adv.Add("target Paralyzed");
        if (target.Conditions.Contains(Condition.Petrified))  adv.Add("target Petrified");
        if (target.Conditions.Contains(Condition.Unconscious) || (target.Type == CombatantType.PC && target.IsUnconscious))
            adv.Add("target Unconscious");
        // Prone: easier in melee, harder at range.
        if (target.Conditions.Contains(Condition.Prone))
            (melee ? adv : dis).Add(melee ? "target Prone (melee)" : "target Prone (ranged)");

        // Attacker's own state gives disadvantage.
        if (attacker.Conditions.Contains(Condition.Blinded))    dis.Add("attacker Blinded");
        if (attacker.Conditions.Contains(Condition.Frightened)) dis.Add("attacker Frightened");
        if (attacker.Conditions.Contains(Condition.Poisoned))   dis.Add("attacker Poisoned");
        if (attacker.Conditions.Contains(Condition.Restrained)) dis.Add("attacker Restrained");
        if (attacker.Conditions.Contains(Condition.Prone))      dis.Add("attacker Prone");

        // Invisibility cuts both ways.
        if (attacker.Conditions.Contains(Condition.Invisible)) adv.Add("attacker Invisible");
        if (target.Conditions.Contains(Condition.Invisible))   dis.Add("target Invisible");

        // The DM's manual toggle folds in as one more source on its side.
        if (manual == AdvantageMode.Advantage)    adv.Add("DM call");
        if (manual == AdvantageMode.Disadvantage) dis.Add("DM call");

        bool hasAdv = adv.Count > 0, hasDis = dis.Count > 0;
        if (hasAdv && !hasDis) return (AdvantageMode.Advantage, string.Join(", ", adv));
        if (hasDis && !hasAdv) return (AdvantageMode.Disadvantage, string.Join(", ", dis));
        if (hasAdv && hasDis)  return (AdvantageMode.Normal, "adv & disadv cancel");
        return (AdvantageMode.Normal, "");
    }

    /// <summary>Heuristic: an attack is melee if its range is Touch/Self/Melee or ≤ 5 ft.</summary>
    public static bool IsMelee(CombatAction action)
    {
        var r = (action.Range ?? "").ToLowerInvariant();
        if (r.Contains("touch") || r.Contains("self") || r.Contains("melee")) return true;
        var m = Regex.Match(r, @"\d+");
        if (m.Success && int.TryParse(m.Value, out var ft)) return ft <= 5;
        return true;   // default to melee when unspecified
    }
}
