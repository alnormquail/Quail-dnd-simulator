using CopilotTest.Models;
using CopilotTest.Models.Content;

namespace CopilotTest.Services;

/// <summary>
/// Applies official content choices (species, later: backgrounds/feats) to a
/// character, auto-applying their effects — the "D&D Beyond feel."
///
/// Provenance strategy: the content library is the source of truth for what a
/// choice grants. We tag everything a species adds with Source = the species'
/// display name, so swapping or clearing a species removes exactly what it
/// added — no per-character delta storage needed.
/// </summary>
public class ContentService
{
    /// <summary>
    /// Set (or clear, if key is null/empty) a character's species, removing the
    /// previous species' contributions and applying the new one's. Mutates the
    /// character in place; the caller is responsible for persisting it.
    /// </summary>
    public void ApplySpecies(Character character, string? speciesKey)
    {
        // Remove anything the previously-selected species contributed.
        var previous = ContentLibrary.GetSpecies(NullIfEmpty(character.SpeciesKey));
        if (previous != null)
            RemoveSpeciesContributions(character, previous);

        var next = ContentLibrary.GetSpecies(NullIfEmpty(speciesKey));
        if (next == null)
        {
            character.SpeciesKey = string.Empty;
            return;
        }

        // Apply the new species.
        character.SpeciesKey = next.Key;
        character.Race       = next.Name;
        character.Speed      = next.Speed;

        foreach (var trait in next.Traits)
        {
            character.Features.Add(new CharacterFeature
            {
                CharacterId = character.Id,
                Name        = trait.Name,
                Description = trait.Description,
                Source      = next.Name,
                LevelGained = 1,
            });
        }

        foreach (var skill in next.SkillProficiencies)
        {
            var existing = character.Skills.FirstOrDefault(s => s.Skill == skill);
            if (existing == null)
            {
                character.Skills.Add(new CharacterSkill
                {
                    CharacterId = character.Id,
                    Skill       = skill,
                    Proficiency = ProficiencyLevel.Proficient,
                });
            }
            // If they already have it (e.g. from class), leave it — don't downgrade.
        }
    }

    // ── Backgrounds ──────────────────────────────────────────────────────────

    /// <summary>
    /// Set (or clear) a character's background, applying its ability bonuses
    /// (per the player's allocation), skill/tool proficiencies, and granted
    /// Origin feat. Reverses the previous background's contributions first.
    /// </summary>
    public void ApplyBackground(Character character, string? backgroundKey, IReadOnlyList<AbilityBonus> abilityChoice)
    {
        var previous = ContentLibrary.GetBackground(NullIfEmpty(character.BackgroundKey));
        if (previous != null)
            RemoveBackgroundContributions(character, previous);

        var next = ContentLibrary.GetBackground(NullIfEmpty(backgroundKey));
        if (next == null)
        {
            character.BackgroundKey = string.Empty;
            return;
        }

        character.BackgroundKey = next.Key;
        character.Background     = next.Name;

        // Ability score bonuses (tagged by background name for clean reversal).
        foreach (var bonus in abilityChoice)
            AddAbilityBonus(character, bonus.Ability, bonus.Amount, next.Name);

        // Skill proficiencies.
        foreach (var skill in next.SkillProficiencies)
            AddSkillIfMissing(character, skill);

        // Tool proficiency shown as a trait for visibility.
        if (!string.IsNullOrEmpty(next.ToolProficiency))
            character.Features.Add(new CharacterFeature
            {
                CharacterId = character.Id,
                Name        = $"Tool Proficiency: {next.ToolProficiency}",
                Description = $"Granted by the {next.Name} background.",
                Source      = next.Name,
                LevelGained = 1,
            });

        // Granted Origin feat — tag its features with the background's name so
        // they're removed together when the background is swapped.
        var feat = ContentLibrary.GetFeat(next.OriginFeatKey);
        if (feat != null)
            ApplyFeatTraits(character, feat, source: next.Name, prefix: $"Origin Feat: {feat.Name} — ");
    }

    private void RemoveBackgroundContributions(Character character, BackgroundData background)
    {
        character.Features.RemoveAll(f => f.Source == background.Name);
        character.AbilityGrants.RemoveAll(g => g.Source == background.Name);
        foreach (var skill in background.SkillProficiencies)
        {
            var match = character.Skills.FirstOrDefault(
                s => s.Skill == skill && s.Proficiency == ProficiencyLevel.Proficient);
            if (match != null) character.Skills.Remove(match);
        }
    }

    // ── Items / equipment ──────────────────────────────────────────────────────

    /// <summary>
    /// Add a library item to the character. Weapons also create a ready-to-use
    /// CombatAction; armor and shields are equipped and recompute AC.
    /// </summary>
    public void AddItemFromLibrary(Character character, ItemData item)
    {
        var inv = new InventoryItem
        {
            CharacterId = character.Id,
            Name        = item.Name,
            Quantity    = 1,
            Weight      = item.Weight > 0 ? item.Weight.ToString("0.#") : "",
            Category    = item.ToCategory(),
            Description = item.Description,
            IsEquipped  = item.Kind is ItemKind.Weapon or ItemKind.Armor or ItemKind.Shield,
        };

        // Only one suit of armor equipped at a time.
        if (item.Kind == ItemKind.Armor)
            foreach (var other in character.Inventory.Where(i => i.IsEquipped && IsArmor(i)))
                other.IsEquipped = false;

        character.Inventory.Add(inv);

        if (item.Kind == ItemKind.Weapon)
            character.Actions.Add(BuildWeaponAction(character, item));

        if (item.Kind is ItemKind.Armor or ItemKind.Shield)
            RecalculateArmorClass(character);
    }

    /// <summary>Recompute AC from the character's equipped armor + shield (5e formula).</summary>
    public void RecalculateArmorClass(Character character)
    {
        var dex = character.DexterityModifier;

        var armor = character.Inventory
            .Where(i => i.IsEquipped)
            .Select(i => ItemLibrary.All.FirstOrDefault(d => d.Name == i.Name && d.Kind == ItemKind.Armor))
            .FirstOrDefault(d => d != null);

        var hasShield = character.Inventory.Any(i => i.IsEquipped &&
            ItemLibrary.All.Any(d => d.Name == i.Name && d.Kind == ItemKind.Shield));

        int ac = armor?.ArmorWeight switch
        {
            ArmorWeight.Light  => armor.BaseAC + dex,
            ArmorWeight.Medium => armor.BaseAC + Math.Min(dex, 2),
            ArmorWeight.Heavy  => armor.BaseAC,
            _                  => 10 + dex,   // unarmored
        };
        if (hasShield) ac += 2;
        character.ArmorClass = ac;
    }

    private static bool IsArmor(InventoryItem i) =>
        ItemLibrary.All.Any(d => d.Name == i.Name && d.Kind == ItemKind.Armor);

    private static CombatAction BuildWeaponAction(Character character, ItemData item)
    {
        var useDex = item.Ranged || (item.Finesse && character.DexterityModifier > character.StrengthModifier);
        var mod = useDex ? character.DexterityModifier : character.StrengthModifier;
        return new CombatAction
        {
            Name        = item.Name,
            ActionType  = ActionType.Attack,
            AttackBonus = mod + character.ProficiencyBonus,
            DamageDice  = item.DamageDice,
            DamageBonus = mod,
            DamageType  = item.DamageType,
            Range       = item.RangeText,
        };
    }

    // ── Subclasses ───────────────────────────────────────────────────────────

    /// <summary>
    /// Set (or clear) a character's subclass, granting all of its features up to
    /// the character's current level. Reverses the previous subclass first.
    /// Features are tagged with the subclass name for clean removal.
    /// </summary>
    public void ApplySubclass(Character character, string? subclassKey)
    {
        var previous = ContentLibrary.GetSubclass(NullIfEmpty(character.SubclassKey));
        if (previous != null)
            character.Features.RemoveAll(f => f.Source == previous.Name);

        var next = ContentLibrary.GetSubclass(NullIfEmpty(subclassKey));
        if (next == null)
        {
            character.SubclassKey = string.Empty;
            character.Subclass    = string.Empty;
            return;
        }

        character.SubclassKey = next.Key;
        character.Subclass    = next.Name;

        foreach (var feat in next.Features.Where(f => f.Level <= character.CharacterLevel))
            AddSubclassFeature(character, next, feat);
    }

    /// <summary>
    /// Grant the chosen subclass's features that are gained at exactly this level
    /// (used by the level-up wizard). No-op if no subclass is set.
    /// </summary>
    public void GrantSubclassFeaturesForLevel(Character character, int level)
    {
        var sub = ContentLibrary.GetSubclass(NullIfEmpty(character.SubclassKey));
        if (sub == null) return;
        foreach (var feat in sub.Features.Where(f => f.Level == level))
            AddSubclassFeature(character, sub, feat);
    }

    private static void AddSubclassFeature(Character character, SubclassData sub, SubclassFeature feat)
    {
        if (character.Features.Any(f => f.Name == feat.Name && f.Source == sub.Name)) return;
        character.Features.Add(new CharacterFeature
        {
            CharacterId = character.Id,
            Name        = feat.Name,
            Description = feat.Description,
            Source      = sub.Name,
            LevelGained = feat.Level,
        });
    }

    // ── Standalone feats ───────────────────────────────────────────────────────

    /// <summary>Add a feat the player picked directly (not via a background).</summary>
    public void ApplyFeat(Character character, string featKey)
    {
        var feat = ContentLibrary.GetFeat(featKey);
        if (feat == null) return;
        var source = FeatSource(feat.Name);
        if (character.Features.Any(f => f.Source == source)) return; // already has it

        ApplyFeatTraits(character, feat, source, prefix: "");
        foreach (var bonus in feat.AbilityBonuses)
            AddAbilityBonus(character, bonus.Ability, bonus.Amount, source);
    }

    public void RemoveFeat(Character character, string featName)
    {
        var source = FeatSource(featName);
        character.Features.RemoveAll(f => f.Source == source);
        character.AbilityGrants.RemoveAll(g => g.Source == source);
    }

    private static string FeatSource(string featName) => $"Feat: {featName}";

    /// <summary>Adds a feat's traits as features under the given source tag.</summary>
    private static void ApplyFeatTraits(Character character, FeatData feat, string source, string prefix)
    {
        foreach (var trait in feat.Traits)
        {
            character.Features.Add(new CharacterFeature
            {
                CharacterId = character.Id,
                Name        = prefix.Length > 0 && trait == feat.Traits[0] ? prefix + trait.Name : trait.Name,
                Description = trait.Description,
                Source      = source,
                LevelGained = 1,
            });
        }
    }

    // ── Shared helpers ─────────────────────────────────────────────────────────

    private static void AddAbilityBonus(Character character, AbilityScore ability, int amount, string source)
    {
        character.AbilityGrants.Add(new AbilityGrant
        {
            CharacterId = character.Id, Ability = ability, Amount = amount, Source = source,
        });
        SetAbility(character, ability, GetAbility(character, ability) + amount);
    }

    private static void AddSkillIfMissing(Character character, Skill skill)
    {
        if (character.Skills.Any(s => s.Skill == skill)) return;
        character.Skills.Add(new CharacterSkill
        {
            CharacterId = character.Id, Skill = skill, Proficiency = ProficiencyLevel.Proficient,
        });
    }

    private static int GetAbility(Character c, AbilityScore a) => a switch
    {
        AbilityScore.Strength => c.Strength, AbilityScore.Dexterity => c.Dexterity,
        AbilityScore.Constitution => c.Constitution, AbilityScore.Intelligence => c.Intelligence,
        AbilityScore.Wisdom => c.Wisdom, AbilityScore.Charisma => c.Charisma, _ => 10,
    };

    private static void SetAbility(Character c, AbilityScore a, int v)
    {
        switch (a)
        {
            case AbilityScore.Strength: c.Strength = v; break;
            case AbilityScore.Dexterity: c.Dexterity = v; break;
            case AbilityScore.Constitution: c.Constitution = v; break;
            case AbilityScore.Intelligence: c.Intelligence = v; break;
            case AbilityScore.Wisdom: c.Wisdom = v; break;
            case AbilityScore.Charisma: c.Charisma = v; break;
        }
    }

    private static void RemoveSpeciesContributions(Character character, SpeciesData species)
    {
        // Remove features sourced from this species.
        character.Features.RemoveAll(f => f.Source == species.Name);

        // Remove only skill proficiencies this species granted (and only if they're
        // still plain "Proficient" — don't strip Expertise the player set elsewhere).
        foreach (var skill in species.SkillProficiencies)
        {
            var match = character.Skills.FirstOrDefault(
                s => s.Skill == skill && s.Proficiency == ProficiencyLevel.Proficient);
            if (match != null) character.Skills.Remove(match);
        }
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
