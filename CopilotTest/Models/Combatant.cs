namespace CopilotTest.Models;

/// <summary>Which damage-modifier bucket a toggle targets.</summary>
public enum DamageMod { Resistance, Immunity, Vulnerability }

public class Combatant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public CombatantType Type { get; set; }

    // Class and level (for PDF-imported characters)
    public string CharacterClass { get; set; } = string.Empty;
    public int CharacterLevel { get; set; } = 0;
    public string ClassDisplay => CharacterLevel > 0 && !string.IsNullOrEmpty(CharacterClass)
        ? $"{CharacterClass} {CharacterLevel}"
        : CharacterClass;

    // Ability scores
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Charisma { get; set; } = 10;

    // Combat stats
    public int MaxHitPoints { get; set; } = 10;
    public int CurrentHitPoints { get; set; } = 10;
    public int TemporaryHitPoints { get; set; } = 0;
    public int ArmorClass { get; set; } = 10;
    public int Speed { get; set; } = 30;
    public int ProficiencyBonus { get; set; } = 2;

    // Initiative
    public int Initiative { get; set; } = 0;
    public int InitiativeRoll { get; set; } = 0;

    // Actions (attacks, spells, abilities)
    public List<CombatAction> Actions { get; set; } = new();

    // Convenience helpers — use primary action if one exists
    public CombatAction? PrimaryAction => Actions.FirstOrDefault(a => a.CanUse) ?? Actions.FirstOrDefault();
    public string AttackName => PrimaryAction?.Name ?? "Attack";
    public int AttackBonus => PrimaryAction?.AttackBonus ?? 0;
    public string DamageDice => PrimaryAction?.DamageDice ?? "1d6";
    public int DamageBonus => PrimaryAction?.DamageBonus ?? 0;

    // Status
    public bool IsAlive => CurrentHitPoints > 0 || HasDeathSaveSuccesses;
    public bool IsUnconscious => CurrentHitPoints <= 0 && Type == CombatantType.PC;
    public bool IsDead { get; set; } = false;

    // Death saving throws (PCs only)
    public int DeathSaveSuccesses { get; set; } = 0;
    public int DeathSaveFailures { get; set; } = 0;
    public bool HasDeathSaveSuccesses => DeathSaveSuccesses < 3 && DeathSaveFailures < 3;

    // Conditions
    public HashSet<Condition> Conditions { get; set; } = new();

    /// <summary>Timed, source-tagged effects (spells/conditions with durations). The
    /// engine mirrors any condition an effect imposes into <see cref="Conditions"/>.</summary>
    public List<ActiveEffect> Effects { get; set; } = new();

    // ── Damage modifiers (applied in CombatEngineService.ApplyDamage) ───────
    public HashSet<DamageType> Resistances { get; set; } = new();     // half damage
    public HashSet<DamageType> Immunities { get; set; } = new();      // no damage
    public HashSet<DamageType> Vulnerabilities { get; set; } = new(); // double damage

    /// <summary>Name of the spell this combatant is concentrating on, or null. Taking
    /// damage triggers a CON save to keep it; dropping to 0 HP ends it.</summary>
    public string? ConcentratingOn { get; set; }

    /// <summary>Class/subclass/species features carried into the encounter (read-only), so the
    /// UI can offer toggles for advantage-granting abilities the character actually has.</summary>
    public List<CharacterFeature> Features { get; set; } = new();

    // ── Live, shared combat resources ───────────────────────────────────────
    /// <summary>Whether this combatant still has its reaction this round (reset each round).</summary>
    public bool ReactionAvailable { get; set; } = true;
    /// <summary>Live play: the DM has hidden this combatant from the players' view
    /// (surprise monsters). Only affects what non-DM seats see in the tracker.</summary>
    public bool IsHiddenFromPlayers { get; set; } = false;
    /// <summary>Spell-slot tiers and how many are spent this encounter (synced to all viewers).</summary>
    public List<SpellSlotState> SpellSlots { get; set; } = new();
    /// <summary>Class resource pools (Lay on Hands, Bardic Inspiration, ...) for live tracking.</summary>
    public List<ResourcePool> Pools { get; set; } = new();

    // ── Barbarian Rage ──────────────────────────────────────────────────────
    /// <summary>True if this character has the Barbarian Rage feature.</summary>
    public bool IsBarbarianClass { get; set; } = false;
    /// <summary>Bonus damage added to melee weapon attacks while raging.</summary>
    public int RageBonus { get; set; } = 0;
    /// <summary>Number of rages available per long rest (from class level).</summary>
    public int RageUsesPerDay { get; set; } = 0;
    /// <summary>Rages still available this combat/day.</summary>
    public int RageUsesRemaining { get; set; } = 0;
    /// <summary>Whether the barbarian is currently raging.</summary>
    public bool IsRaging { get; set; } = false;

    // Display helpers
    public int StrengthModifier => GetModifier(Strength);
    public int DexterityModifier => GetModifier(Dexterity);
    public int ConstitutionModifier => GetModifier(Constitution);
    public int IntelligenceModifier => GetModifier(Intelligence);
    public int WisdomModifier => GetModifier(Wisdom);
    public int CharismaModifier => GetModifier(Charisma);

    public static int GetModifier(int score) => (int)Math.Floor((score - 10) / 2.0);

    public string HpDisplay => $"{CurrentHitPoints}/{MaxHitPoints}" + (TemporaryHitPoints > 0 ? $" (+{TemporaryHitPoints} temp)" : "");

    public string StatusDisplay
    {
        get
        {
            if (IsDead) return "💀 Dead";
            if (Type == CombatantType.PC && IsUnconscious)
                return $"😵 Unconscious ({DeathSaveSuccesses}✓/{DeathSaveFailures}✗)";
            var rageTag = IsRaging ? " 🔥 Raging" : "";
            if (CurrentHitPoints <= MaxHitPoints / 4) return $"🩸 Critical{rageTag}";
            if (CurrentHitPoints <= MaxHitPoints / 2) return $"🤕 Bloodied{rageTag}";
            return $"✅ Healthy{rageTag}";
        }
    }
}
