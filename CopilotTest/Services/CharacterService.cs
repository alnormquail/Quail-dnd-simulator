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

        // One-time correction of the preloaded casters' spell lists to match
        // their PDF sheets. After it runs once, the party members can be
        // hand-edited freely without their spells reverting.
        if (!MetaFlagSet("preloaded-spells-corrected-v1"))
        {
            CorrectPreloadedSpells();
            SetMetaFlag("preloaded-spells-corrected-v1");
        }

        // One-time load of the preloaded party's inventory from their PDF sheets.
        // Only fills characters whose inventory is currently empty, so it never
        // clobbers gear the user has added.
        if (!MetaFlagSet("preloaded-inventory-loaded-v1"))
        {
            LoadPreloadedInventory();
            SetMetaFlag("preloaded-inventory-loaded-v1");
        }

        // One-time load of the preloaded party's class/species features onto the
        // Stats tab. Only fills characters whose feature list is currently empty.
        if (!MetaFlagSet("preloaded-features-loaded-v1"))
        {
            LoadPreloadedFeatures();
            SetMetaFlag("preloaded-features-loaded-v1");
        }

        // One-time: link the party's subclasses so the sheet's subclass dropdown
        // reflects their actual subclass (only fills characters with none set).
        if (!MetaFlagSet("preloaded-subclass-linked-v2"))
        {
            LinkPreloadedSubclasses();
            SetMetaFlag("preloaded-subclass-linked-v2");
        }
    }

    /// <summary>
    /// One-time: load each preloaded character's inventory (from their PDF
    /// sheet) into the DB, but only for characters whose inventory is empty.
    /// Inserts directly by FK; never touches user-created characters.
    /// </summary>
    private void LoadPreloadedInventory()
    {
        var changed = false;
        foreach (var template in PreloadedCharacters.All.Where(t => t.Inventory.Count > 0))
        {
            if (!_db.Characters.Any(c => c.Id == template.Id)) continue;
            if (_db.InventoryItems.Any(i => i.CharacterId == template.Id)) continue;  // already has gear

            foreach (var item in template.Inventory)
            {
                _db.InventoryItems.Add(new InventoryItem
                {
                    CharacterId = template.Id,
                    Name        = item.Name,
                    Quantity    = item.Quantity,
                    Weight      = item.Weight,
                    Description = item.Description,
                    IsEquipped  = item.IsEquipped,
                    Category    = item.Category,
                });
            }
            changed = true;
        }
        if (changed) _db.SaveChanges();
    }

    /// <summary>
    /// One-time: load each preloaded character's class/species features into the
    /// DB, but only for characters whose feature list is empty. Inserts by FK;
    /// never touches user-created characters.
    /// </summary>
    private void LoadPreloadedFeatures()
    {
        var changed = false;
        foreach (var template in PreloadedCharacters.All.Where(t => t.Features.Count > 0))
        {
            if (!_db.Characters.Any(c => c.Id == template.Id)) continue;
            if (_db.CharacterFeatures.Any(f => f.CharacterId == template.Id)) continue;  // already has features

            foreach (var f in template.Features)
            {
                _db.CharacterFeatures.Add(new CharacterFeature
                {
                    CharacterId = template.Id,
                    Name        = f.Name,
                    Description = f.Description,
                    Source      = f.Source,
                    LevelGained = f.LevelGained,
                });
            }
            changed = true;
        }
        if (changed) _db.SaveChanges();
    }

    /// <summary>
    /// One-time: set SubclassKey + Subclass on preloaded party members that have
    /// none, so the sheet's subclass dropdown shows their actual subclass. Raw
    /// UPDATE (case-insensitive Id match) to avoid EF concurrency on startup.
    /// </summary>
    private void LinkPreloadedSubclasses()
    {
        foreach (var t in PreloadedCharacters.All.Where(c => !string.IsNullOrEmpty(c.SubclassKey)))
        {
            _db.Database.ExecuteSqlRaw(
                "UPDATE \"Characters\" SET \"SubclassKey\" = {0}, \"Subclass\" = {1} " +
                "WHERE \"Id\" = {2} COLLATE NOCASE AND (\"SubclassKey\" = '' OR \"SubclassKey\" IS NULL);",
                t.SubclassKey, t.Subclass, t.Id.ToString());
        }
    }

    private bool MetaFlagSet(string key)
    {
        using var cmd = _db.Database.GetDbConnection().CreateCommand();
        if (cmd.Connection!.State != System.Data.ConnectionState.Open) cmd.Connection.Open();
        cmd.CommandText = "SELECT COUNT(*) FROM AppMeta WHERE \"Key\" = $k;";
        var p = cmd.CreateParameter(); p.ParameterName = "$k"; p.Value = key; cmd.Parameters.Add(p);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    private void SetMetaFlag(string key) =>
        _db.Database.ExecuteSqlRaw(
            "INSERT OR IGNORE INTO \"AppMeta\" (\"Key\", \"Value\") VALUES ({0}, {1});",
            key, DateTime.UtcNow.ToString("o"));

    /// <summary>
    /// One-time: bring the preloaded casters' spell lists into line with their
    /// PDF-sourced templates (replaces a caster's spells when they differ).
    /// Gated by an AppMeta flag so it runs once and then leaves the party
    /// members editable. Never touches user-created characters.
    /// </summary>
    private void CorrectPreloadedSpells()
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
