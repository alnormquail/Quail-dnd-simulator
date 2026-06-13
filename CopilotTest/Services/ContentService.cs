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
