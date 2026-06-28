namespace CopilotTest.Models;

public enum ActionType
{
    Attack,       // Standard weapon attack roll
    Spell,        // Spell that requires a save (no attack roll)
    SpellAttack,  // Spell that uses an attack roll
    Other         // Utility / special ability
}

public enum DamageType
{
    None,
    Slashing, Piercing, Bludgeoning,
    Fire, Cold, Lightning, Thunder, Acid, Poison, Necrotic, Radiant, Psychic, Force
}

public enum AbilityScore { Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma }

/// <summary>How an attack's d20 is rolled — set per attack at the DM's discretion.</summary>
public enum AdvantageMode { Normal, Advantage, Disadvantage }

public class CombatAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CharacterId { get; set; }
    public string Name { get; set; } = "Attack";
    public ActionType ActionType { get; set; } = ActionType.Attack;

    // Attack roll actions
    public int AttackBonus { get; set; } = 0;

    // Damage
    public string DamageDice { get; set; } = "1d6";
    public int DamageBonus { get; set; } = 0;
    public DamageType DamageType { get; set; } = DamageType.None;

    // Spell save actions
    public int SaveDC { get; set; } = 0;
    public AbilityScore SaveAbility { get; set; } = AbilityScore.Dexterity;

    // Spell metadata
    public int SpellLevel { get; set; } = 0;  // 0 = cantrip
    public string Range { get; set; } = "5 ft";
    public string Description { get; set; } = string.Empty;

    // Usage limits (for abilities / spell slots)
    public int UsesPerDay { get; set; } = 0;   // 0 = unlimited
    public int UsesRemaining { get; set; } = 0;

    public bool IsLimited => UsesPerDay > 0;
    public bool CanUse => !IsLimited || UsesRemaining > 0;

    public string DisplayName => SpellLevel > 0 ? $"{Name} (Lvl {SpellLevel})" : Name;

    private string DamageText =>
        string.IsNullOrEmpty(DamageDice) ? "" :
        $"{DamageDice}{(DamageBonus != 0 ? (DamageBonus > 0 ? $"+{DamageBonus}" : $"{DamageBonus}") : "")} {DamageType} dmg";

    public string ShortSummary => ActionType switch
    {
        ActionType.Attack or ActionType.SpellAttack =>
            $"+{AttackBonus} to hit{(DamageText.Length > 0 ? $", {DamageText}" : "")}",
        // Save spell only when it actually has a save DC.
        ActionType.Spell when SaveDC > 0 =>
            $"DC {SaveDC} {SaveAbility} save{(DamageText.Length > 0 ? $", {DamageText}" : "")}",
        // Damaging spell with no save (e.g. Divine Smite): just the damage.
        ActionType.Spell when DamageText.Length > 0 => DamageText,
        // Buff/utility spell: fall back to the description, else a dash.
        ActionType.Spell => Description.Length > 0 ? (Description.Length > 40 ? Description[..40] + "…" : Description) : "—",
        _ => Description.Length > 40 ? Description[..40] + "…" : Description
    };
}
