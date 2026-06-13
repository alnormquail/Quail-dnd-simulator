using Microsoft.EntityFrameworkCore;
using CopilotTest.Models;

namespace CopilotTest.Data;

public class DndDbContext : DbContext
{
    public DndDbContext(DbContextOptions<DndDbContext> options) : base(options) { }

    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CombatAction> CombatActions => Set<CombatAction>();
    public DbSet<Spell> Spells => Set<Spell>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<CharacterSkill> CharacterSkills => Set<CharacterSkill>();
    public DbSet<SpellSlot> SpellSlots => Set<SpellSlot>();
    public DbSet<CharacterFeature> CharacterFeatures => Set<CharacterFeature>();
    public DbSet<AbilityGrant> AbilityGrants => Set<AbilityGrant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Character>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasMany(c => c.Actions)
             .WithOne()
             .HasForeignKey(a => a.CharacterId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(c => c.Spells)
             .WithOne()
             .HasForeignKey(s => s.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(c => c.Inventory)
             .WithOne()
             .HasForeignKey(i => i.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(c => c.Skills)
             .WithOne()
             .HasForeignKey(s => s.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(c => c.SpellSlots)
             .WithOne()
             .HasForeignKey(s => s.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(c => c.Features)
             .WithOne()
             .HasForeignKey(f => f.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(c => c.AbilityGrants)
             .WithOne()
             .HasForeignKey(g => g.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AbilityGrant>()
            .Property(g => g.Ability)
            .HasConversion<string>();

        // Enums stored as strings for readability
        modelBuilder.Entity<CharacterSkill>()
            .Property(s => s.Skill)
            .HasConversion<string>();
        modelBuilder.Entity<CharacterSkill>()
            .Property(s => s.Proficiency)
            .HasConversion<string>();
        modelBuilder.Entity<CombatAction>()
            .Property(a => a.ActionType)
            .HasConversion<string>();
        modelBuilder.Entity<CombatAction>()
            .Property(a => a.DamageType)
            .HasConversion<string>();
        modelBuilder.Entity<CombatAction>()
            .Property(a => a.SaveAbility)
            .HasConversion<string>();
        modelBuilder.Entity<Character>()
            .Property(c => c.Type)
            .HasConversion<string>();
        modelBuilder.Entity<InventoryItem>()
            .Property(i => i.Category)
            .HasConversion<string>();
    }
}
