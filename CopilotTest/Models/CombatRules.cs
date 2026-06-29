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

        // Standing-advantage effects (Reckless Attack, Innate Sorcery, ...).
        foreach (var e in attacker.Effects)
            if (e.AdvantageOnOwnAttacks && EffectAppliesToAttack(e, action, melee)) adv.Add(e.Name);
        foreach (var e in target.Effects)
            if (e.AdvantageToAttackers) adv.Add($"target {e.Name}");

        // The DM's manual toggle folds in as one more source on its side.
        if (manual == AdvantageMode.Advantage)    adv.Add("DM call");
        if (manual == AdvantageMode.Disadvantage) dis.Add("DM call");

        bool hasAdv = adv.Count > 0, hasDis = dis.Count > 0;
        if (hasAdv && !hasDis) return (AdvantageMode.Advantage, string.Join(", ", adv));
        if (hasDis && !hasAdv) return (AdvantageMode.Disadvantage, string.Join(", ", dis));
        if (hasAdv && hasDis)  return (AdvantageMode.Normal, "adv & disadv cancel");
        return (AdvantageMode.Normal, "");
    }

    /// <summary>Does a standing-advantage effect apply to this particular attack?</summary>
    private static bool EffectAppliesToAttack(ActiveEffect e, CombatAction action, bool melee) => e.AppliesTo switch
    {
        AttackKind.Any    => true,
        AttackKind.Melee  => action.ActionType == ActionType.Attack && melee,
        AttackKind.Ranged => action.ActionType == ActionType.Attack && !melee,
        AttackKind.Spell  => action.ActionType is ActionType.SpellAttack or ActionType.Spell,
        _                 => false,
    };

    /// <summary>
    /// Known abilities that grant standing advantage, keyed by the character-feature name
    /// that grants them. The live UI offers a toggle for each of these a combatant has.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, AdvantageAbility> AdvantageAbilities =
        new Dictionary<string, AdvantageAbility>
        {
            ["Reckless Attack"] = new("Reckless Attack", OnOwnAttacks: true, ToAttackers: true, AttackKind.Melee, Rounds: 1,
                "Advantage on your melee attacks this turn; attacks against you have advantage until your next turn."),
            ["Innate Sorcery"] = new("Innate Sorcery", OnOwnAttacks: true, ToAttackers: false, AttackKind.Spell, Rounds: 10,
                "Advantage on your spell attacks for 1 minute. (The +1 spell save DC is tracked by you.)"),
        };

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

/// <summary>A togglable ability that grants standing advantage while active.</summary>
public record AdvantageAbility(string Name, bool OnOwnAttacks, bool ToAttackers, AttackKind AppliesTo, int Rounds, string Blurb);
