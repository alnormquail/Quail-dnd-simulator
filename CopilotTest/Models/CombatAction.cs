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

public class CombatAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
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

    public string ShortSummary => ActionType switch
    {
        ActionType.Attack or ActionType.SpellAttack =>
            $"+{AttackBonus} to hit, {DamageDice}{(DamageBonus != 0 ? (DamageBonus > 0 ? $"+{DamageBonus}" : $"{DamageBonus}") : "")} {DamageType} dmg",
        ActionType.Spell =>
            $"DC {SaveDC} {SaveAbility} save, {DamageDice}{(DamageBonus != 0 ? (DamageBonus > 0 ? $"+{DamageBonus}" : $"{DamageBonus}") : "")} {DamageType} dmg",
        _ => Description.Length > 40 ? Description[..40] + "…" : Description
    };
}
