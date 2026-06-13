using Microsoft.EntityFrameworkCore;
using CopilotTest.Data;
using CopilotTest.Models;

namespace CopilotTest.Services;

public class CharacterService
{
    private readonly DndDbContext _db;

    public CharacterService(DndDbContext db)
    {
        _db = db;
    }

    public List<Character> GetAll() =>
        _db.Characters
           .Include(c => c.Actions)
           .Include(c => c.Spells)
           .Include(c => c.Inventory)
           .Include(c => c.Skills)
           .Include(c => c.SpellSlots)
           .Include(c => c.Features)
           .Include(c => c.AbilityGrants)
           .OrderBy(c => c.Name)
           .ToList();

    public List<Character> GetPCs() =>
        _db.Characters
           .Include(c => c.Actions)
           .Include(c => c.Spells)
           .Include(c => c.Inventory)
           .Include(c => c.Skills)
           .Include(c => c.SpellSlots)
           .Include(c => c.Features)
           .Include(c => c.AbilityGrants)
           .Where(c => c.Type != CombatantType.Monster)
           .OrderBy(c => c.Name)
           .ToList();

    public Character? GetById(Guid id) =>
        _db.Characters
           .Include(c => c.Actions)
           .Include(c => c.Spells)
           .Include(c => c.Inventory)
           .Include(c => c.Skills)
           .Include(c => c.SpellSlots)
           .Include(c => c.Features)
           .Include(c => c.AbilityGrants)
           .FirstOrDefault(c => c.Id == id);

    public Character Create(Character character)
    {
        _db.Characters.Add(character);
        _db.SaveChanges();
        return character;
    }

    public void Update(Character character)
    {
        var existing = _db.Characters
            .Include(c => c.Actions)
            .Include(c => c.Spells)
            .Include(c => c.Inventory)
            .Include(c => c.Skills)
            .Include(c => c.SpellSlots)
            .Include(c => c.Features)
            .Include(c => c.AbilityGrants)
            .FirstOrDefault(c => c.Id == character.Id);

        if (existing == null) return;

        // Update scalar fields
        _db.Entry(existing).CurrentValues.SetValues(character);

        // Replace collections
        ReplaceCollection(_db.CombatActions, existing.Actions, character.Actions, a => { a.CharacterId = character.Id; });
        ReplaceCollection(_db.Spells, existing.Spells, character.Spells, s => { s.CharacterId = character.Id; });
        ReplaceCollection(_db.InventoryItems, existing.Inventory, character.Inventory, i => { i.CharacterId = character.Id; });
        ReplaceCollection(_db.CharacterSkills, existing.Skills, character.Skills, s => { s.CharacterId = character.Id; });
        ReplaceCollection(_db.SpellSlots, existing.SpellSlots, character.SpellSlots, s => { s.CharacterId = character.Id; });
        ReplaceCollection(_db.CharacterFeatures, existing.Features, character.Features, f => { f.CharacterId = character.Id; });
        ReplaceCollection(_db.AbilityGrants, existing.AbilityGrants, character.AbilityGrants, g => { g.CharacterId = character.Id; });

        _db.SaveChanges();
    }

    /// <summary>Updates slot usage only (expend/restore) without a full character save.</summary>
    public void SaveSpellSlots(Guid characterId, List<SpellSlot> slots)
    {
        var existing = _db.SpellSlots.Where(s => s.CharacterId == characterId).ToList();
        foreach (var slot in slots)
        {
            var match = existing.FirstOrDefault(s => s.Level == slot.Level);
            if (match != null) match.UsedSlots = slot.UsedSlots;
        }
        _db.SaveChanges();
    }

    public void Delete(Guid id)
    {
        var character = _db.Characters.Find(id);
        if (character != null)
        {
            _db.Characters.Remove(character);
            _db.SaveChanges();
        }
    }

    public bool Exists(Guid id) => _db.Characters.Any(c => c.Id == id);

    /// <summary>Seed preloaded party members, adding any that are missing from the DB.</summary>
    public void SeedIfEmpty()
    {
        var existingIds = _db.Characters.Select(c => c.Id).ToHashSet();
        foreach (var c in PreloadedCharacters.All)
        {
            if (!existingIds.Contains(c.Id))
                _db.Characters.Add(c);
        }

        // One-time correction: Winnie was originally seeded with stats that
        // didn't match her PDF sheet (HP 31 instead of 32).
        var winnie = _db.Characters.FirstOrDefault(c =>
            c.Id == new Guid("a1000000-0000-0000-0000-000000000004") && c.MaxHitPoints == 31);
        if (winnie != null)
        {
            winnie.MaxHitPoints = 32;
            winnie.Dexterity    = 13;
            winnie.Intelligence = 14;
        }

        _db.SaveChanges();

        SyncPreloadedSpells();
    }

    /// <summary>
    /// Keep the preloaded casters' spell lists in sync with their templates
    /// (the authoritative party data from their PDF sheets). If a caster's DB
    /// spell set differs from the template, it's replaced. Runs every startup
    /// but is a no-op once in sync.
    ///
    /// Note: this makes the templates authoritative for the preloaded party, so
    /// manual spell edits to those specific characters would revert on restart.
    /// New (user-created) characters are never touched.
    /// </summary>
    private void SyncPreloadedSpells()
    {
        var changed = false;
        foreach (var template in PreloadedCharacters.All.Where(t => t.Spells.Count > 0))
        {
            if (!_db.Characters.Any(c => c.Id == template.Id)) continue;

            var existing = _db.Spells.Where(s => s.CharacterId == template.Id).ToList();
            var existingNames = existing.Select(s => s.Name).OrderBy(n => n).ToList();
            var templateNames = template.Spells.Select(s => s.Name).OrderBy(n => n).ToList();
            if (existingNames.SequenceEqual(templateNames)) continue;   // already in sync

            _db.Spells.RemoveRange(existing);
            foreach (var s in template.Spells)
            {
                _db.Spells.Add(new Spell
                {
                    CharacterId   = template.Id,
                    Name          = s.Name,
                    Level         = s.Level,
                    School        = s.School,
                    CastingTime   = s.CastingTime,
                    Range         = s.Range,
                    Components    = s.Components,
                    Duration      = s.Duration,
                    Concentration = s.Concentration,
                    IsRitual      = s.IsRitual,
                    IsPrepared    = s.IsPrepared,
                    Description   = s.Description,
                });
            }
            changed = true;
        }
        if (changed) _db.SaveChanges();
    }

    private static void ReplaceCollection<T>(DbSet<T> dbSet, ICollection<T> existing, ICollection<T> incoming, Action<T> setFk)
        where T : class
    {
        dbSet.RemoveRange(existing);
        foreach (var item in incoming)
        {
            setFk(item);
            dbSet.Add(item);
        }
    }
}
