using Microsoft.EntityFrameworkCore;
using CopilotTest.Data;
using CopilotTest.Models;

namespace CopilotTest.Services;

/// <summary>
/// Character persistence. Uses an <see cref="IDbContextFactory{TContext}"/> so
/// every operation gets a FRESH, short-lived DbContext. This avoids the
/// Blazor-Server pitfall of one long-lived scoped context per circuit
/// accumulating stale tracking state (which caused UNIQUE / concurrency errors).
/// </summary>
public class CharacterService
{
    private readonly IDbContextFactory<DndDbContext> _factory;

    public CharacterService(IDbContextFactory<DndDbContext> factory) => _factory = factory;

    private static IQueryable<Character> WithChildren(DndDbContext db) =>
        db.Characters
          .Include(c => c.Actions)
          .Include(c => c.Spells)
          .Include(c => c.Inventory)
          .Include(c => c.Skills)
          .Include(c => c.SpellSlots)
          .Include(c => c.Features)
          .Include(c => c.AbilityGrants);

    public List<Character> GetAll()
    {
        using var db = _factory.CreateDbContext();
        return WithChildren(db).AsNoTracking().OrderBy(c => c.Name).ToList();
    }

    public List<Character> GetPCs()
    {
        using var db = _factory.CreateDbContext();
        return WithChildren(db).AsNoTracking()
            .Where(c => c.Type != CombatantType.Monster).OrderBy(c => c.Name).ToList();
    }

    public Character? GetById(Guid id)
    {
        using var db = _factory.CreateDbContext();
        return WithChildren(db).AsNoTracking().FirstOrDefault(c => c.Id == id);
    }

    public Character Create(Character character)
    {
        using var db = _factory.CreateDbContext();
        db.Characters.Add(character);
        db.SaveChanges();
        return character;
    }

    /// <summary>
    /// Persist a (detached) character graph. Loads the tracked row, copies scalar
    /// fields, and replaces each child collection. Incoming children are given
    /// fresh Ids so the inserts never collide with the rows being deleted.
    /// </summary>
    public void Update(Character character)
    {
        using var db = _factory.CreateDbContext();
        var existing = WithChildren(db).FirstOrDefault(c => c.Id == character.Id);
        if (existing == null) return;

        db.Entry(existing).CurrentValues.SetValues(character);

        ReplaceCollection(db.CombatActions, existing.Actions, character.Actions, a => { a.Id = Guid.NewGuid(); a.CharacterId = character.Id; });
        ReplaceCollection(db.Spells, existing.Spells, character.Spells, s => { s.Id = Guid.NewGuid(); s.CharacterId = character.Id; });
        ReplaceCollection(db.InventoryItems, existing.Inventory, character.Inventory, i => { i.Id = Guid.NewGuid(); i.CharacterId = character.Id; });
        ReplaceCollection(db.CharacterSkills, existing.Skills, character.Skills, s => { s.Id = 0; s.CharacterId = character.Id; });
        ReplaceCollection(db.SpellSlots, existing.SpellSlots, character.SpellSlots, s => { s.Id = 0; s.CharacterId = character.Id; });
        ReplaceCollection(db.CharacterFeatures, existing.Features, character.Features, f => { f.Id = Guid.NewGuid(); f.CharacterId = character.Id; });
        ReplaceCollection(db.AbilityGrants, existing.AbilityGrants, character.AbilityGrants, g => { g.Id = Guid.NewGuid(); g.CharacterId = character.Id; });

        db.SaveChanges();
    }

    /// <summary>Updates slot usage only (expend/restore) without a full character save.</summary>
    public void SaveSpellSlots(Guid characterId, List<SpellSlot> slots)
    {
        using var db = _factory.CreateDbContext();
        var existing = db.SpellSlots.Where(s => s.CharacterId == characterId).ToList();
        foreach (var slot in slots)
        {
            var match = existing.FirstOrDefault(s => s.Level == slot.Level);
            if (match != null) match.UsedSlots = slot.UsedSlots;
        }
        db.SaveChanges();
    }

    public void Delete(Guid id)
    {
        using var db = _factory.CreateDbContext();
        var character = db.Characters.Find(id);
        if (character != null)
        {
            db.Characters.Remove(character);
            db.SaveChanges();
        }
    }

    public bool Exists(Guid id)
    {
        using var db = _factory.CreateDbContext();
        return db.Characters.Any(c => c.Id == id);
    }

    /// <summary>Seed preloaded party members, adding any that are missing, then run one-time data fixes.</summary>
    public void SeedIfEmpty()
    {
        using var db = _factory.CreateDbContext();

        var existingIds = db.Characters.Select(c => c.Id).ToHashSet();
        foreach (var c in PreloadedCharacters.All)
            if (!existingIds.Contains(c.Id)) db.Characters.Add(c);

        // One-time stat correction for Winnie (PDF said HP 32, not the original 31).
        var winnie = db.Characters.FirstOrDefault(c =>
            c.Id == new Guid("a1000000-0000-0000-0000-000000000004") && c.MaxHitPoints == 31);
        if (winnie != null)
        {
            winnie.MaxHitPoints = 32;
            winnie.Dexterity    = 13;
            winnie.Intelligence = 14;
        }

        db.SaveChanges();

        if (!MetaFlagSet(db, "preloaded-spells-corrected-v1"))
        {
            CorrectPreloadedSpells(db);
            SetMetaFlag(db, "preloaded-spells-corrected-v1");
        }
        if (!MetaFlagSet(db, "preloaded-inventory-loaded-v1"))
        {
            LoadPreloadedInventory(db);
            SetMetaFlag(db, "preloaded-inventory-loaded-v1");
        }
        if (!MetaFlagSet(db, "preloaded-features-loaded-v1"))
        {
            LoadPreloadedFeatures(db);
            SetMetaFlag(db, "preloaded-features-loaded-v1");
        }
        if (!MetaFlagSet(db, "preloaded-subclass-linked-v2"))
        {
            LinkPreloadedSubclasses(db);
            SetMetaFlag(db, "preloaded-subclass-linked-v2");
        }
    }

    private static void LoadPreloadedInventory(DndDbContext db)
    {
        var changed = false;
        foreach (var template in PreloadedCharacters.All.Where(t => t.Inventory.Count > 0))
        {
            if (!db.Characters.Any(c => c.Id == template.Id)) continue;
            if (db.InventoryItems.Any(i => i.CharacterId == template.Id)) continue;
            foreach (var item in template.Inventory)
                db.InventoryItems.Add(new InventoryItem
                {
                    CharacterId = template.Id, Name = item.Name, Quantity = item.Quantity,
                    Weight = item.Weight, Description = item.Description, IsEquipped = item.IsEquipped, Category = item.Category,
                });
            changed = true;
        }
        if (changed) db.SaveChanges();
    }

    private static void LoadPreloadedFeatures(DndDbContext db)
    {
        var changed = false;
        foreach (var template in PreloadedCharacters.All.Where(t => t.Features.Count > 0))
        {
            if (!db.Characters.Any(c => c.Id == template.Id)) continue;
            if (db.CharacterFeatures.Any(f => f.CharacterId == template.Id)) continue;
            foreach (var f in template.Features)
                db.CharacterFeatures.Add(new CharacterFeature
                {
                    CharacterId = template.Id, Name = f.Name, Description = f.Description,
                    Source = f.Source, LevelGained = f.LevelGained,
                });
            changed = true;
        }
        if (changed) db.SaveChanges();
    }

    private static void LinkPreloadedSubclasses(DndDbContext db)
    {
        foreach (var t in PreloadedCharacters.All.Where(c => !string.IsNullOrEmpty(c.SubclassKey)))
        {
            db.Database.ExecuteSqlRaw(
                "UPDATE \"Characters\" SET \"SubclassKey\" = {0}, \"Subclass\" = {1} " +
                "WHERE \"Id\" = {2} COLLATE NOCASE AND (\"SubclassKey\" = '' OR \"SubclassKey\" IS NULL);",
                t.SubclassKey, t.Subclass, t.Id.ToString());
        }
    }

    private static bool MetaFlagSet(DndDbContext db, string key)
    {
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        if (cmd.Connection!.State != System.Data.ConnectionState.Open) cmd.Connection.Open();
        cmd.CommandText = "SELECT COUNT(*) FROM AppMeta WHERE \"Key\" = $k;";
        var p = cmd.CreateParameter(); p.ParameterName = "$k"; p.Value = key; cmd.Parameters.Add(p);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    private static void SetMetaFlag(DndDbContext db, string key) =>
        db.Database.ExecuteSqlRaw(
            "INSERT OR IGNORE INTO \"AppMeta\" (\"Key\", \"Value\") VALUES ({0}, {1});",
            key, DateTime.UtcNow.ToString("o"));

    private static void CorrectPreloadedSpells(DndDbContext db)
    {
        var changed = false;
        foreach (var template in PreloadedCharacters.All.Where(t => t.Spells.Count > 0))
        {
            if (!db.Characters.Any(c => c.Id == template.Id)) continue;

            var existing = db.Spells.Where(s => s.CharacterId == template.Id).ToList();
            var existingNames = existing.Select(s => s.Name).OrderBy(n => n).ToList();
            var templateNames = template.Spells.Select(s => s.Name).OrderBy(n => n).ToList();
            if (existingNames.SequenceEqual(templateNames)) continue;

            db.Spells.RemoveRange(existing);
            foreach (var s in template.Spells)
                db.Spells.Add(new Spell
                {
                    CharacterId = template.Id, Name = s.Name, Level = s.Level, School = s.School,
                    CastingTime = s.CastingTime, Range = s.Range, Components = s.Components, Duration = s.Duration,
                    Concentration = s.Concentration, IsRitual = s.IsRitual, IsPrepared = s.IsPrepared, Description = s.Description,
                });
            changed = true;
        }
        if (changed) db.SaveChanges();
    }

    private static void ReplaceCollection<T>(DbSet<T> dbSet, ICollection<T> existing, ICollection<T> incoming, Action<T> prep)
        where T : class
    {
        dbSet.RemoveRange(existing);
        foreach (var item in incoming)
        {
            prep(item);
            dbSet.Add(item);
        }
    }
}
