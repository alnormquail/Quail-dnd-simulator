namespace CopilotTest.Models;

/// <summary>
/// A reversible ability-score bonus applied to a character by some content
/// choice (background, feat, …). Tagged by Source so it can be removed exactly
/// when that choice is swapped away. The bonus is baked into the character's
/// stored score; this record is the receipt that lets us undo it.
/// </summary>
public class AbilityGrant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }

    public AbilityScore Ability { get; set; }
    public int Amount { get; set; }
    /// <summary>What applied it, e.g. "Soldier" or "Feat: Great Weapon Master".</summary>
    public string Source { get; set; } = string.Empty;
}
