using CopilotTest.Data;
using CopilotTest.Models;
using CopilotTest.Models.Content;
using CopilotTest.Services;
using Microsoft.EntityFrameworkCore;

// ───────────────────────── mini test framework ─────────────────────────
int checks = 0;
var failures = new List<string>();
void Check(bool cond, string desc)
{
    checks++;
    if (!cond) { failures.Add(desc); Console.WriteLine($"   ✗ FAIL: {desc}"); }
}
void Note(string msg) => Console.WriteLine($"   • {msg}");
void Section(string name) => Console.WriteLine($"\n══ {name} ══");

// ───────────────────────── throwaway database ─────────────────────────
var dbPath = Path.Combine(Path.GetTempPath(), $"quail-test-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<DndDbContext>().UseSqlite($"Data Source={dbPath}").Options;
var factory = new TestFactory(options);
using (var seed = factory.CreateDbContext()) seed.Database.EnsureCreated();

var svc = new CharacterService(factory);
var content = new ContentService();

Console.WriteLine($"Quail D&D Simulator — headless test run");
Console.WriteLine($"temp db: {dbPath}");

// ───────────────────────── helpers ─────────────────────────

// Replicates CharacterSheet.ApplyLevelUp exactly (minus UI/combat refresh),
// then persists and reloads — so it exercises the real save path.
Character LevelUp(Character ch, string asiFirst = "", string asiSecond = "", List<Spell>? pending = null)
{
    int hpGain = LevelUpRules.AverageHpGain(ch.CharacterClass, ch.ConstitutionModifier);
    int oldConMod = ch.ConstitutionModifier;
    int newLevel = ch.CharacterLevel + 1;

    void Asi(string a)
    {
        if (string.IsNullOrEmpty(a)) return;
        switch (a)
        {
            case "Strength":     ch.Strength     = Math.Min(20, ch.Strength + 1);     break;
            case "Dexterity":    ch.Dexterity    = Math.Min(20, ch.Dexterity + 1);    break;
            case "Constitution": ch.Constitution = Math.Min(20, ch.Constitution + 1); break;
            case "Intelligence": ch.Intelligence = Math.Min(20, ch.Intelligence + 1); break;
            case "Wisdom":       ch.Wisdom       = Math.Min(20, ch.Wisdom + 1);       break;
            case "Charisma":     ch.Charisma     = Math.Min(20, ch.Charisma + 1);     break;
        }
    }
    Asi(asiFirst); Asi(asiSecond);

    ch.CharacterLevel = newLevel;
    ch.MaxHitPoints += hpGain;
    ch.ProficiencyBonus = LevelUpRules.ProficiencyForLevel(newLevel);
    int conInc = ch.ConstitutionModifier - oldConMod;
    if (conInc > 0) ch.MaxHitPoints += conInc * newLevel;

    foreach (var (lvl, max) in LevelUpRules.SlotsForLevel(ch.CharacterClass, newLevel))
    {
        var ex = ch.SpellSlots.FirstOrDefault(s => s.Level == lvl);
        if (ex != null) ex.MaxSlots = Math.Max(ex.MaxSlots, max);
        else ch.SpellSlots.Add(new SpellSlot { CharacterId = ch.Id, Level = lvl, MaxSlots = max });
    }
    if (pending != null)
        foreach (var sp in pending)
            if (!ch.Spells.Any(s => s.Name.Equals(sp.Name, StringComparison.OrdinalIgnoreCase)))
                ch.Spells.Add(sp);

    content.GrantSubclassFeaturesForLevel(ch, newLevel);
    content.RefreshSubclassSpells(ch);

    foreach (var g in LevelUpRules.FeatureGrants(ch.CharacterClass, newLevel))
        if (!ch.Features.Any(f => f.Name.Equals(g.Name, StringComparison.OrdinalIgnoreCase)))
            ch.Features.Add(new CharacterFeature
            {
                CharacterId = ch.Id, Name = g.Name, Description = g.Description,
                Source = $"{ch.CharacterClass} {newLevel}", LevelGained = newLevel,
            });

    svc.Update(ch);
    return svc.GetById(ch.Id)!;
}

// Builds a character with species/background/subclass applied, plus a couple of
// player-owned spells/features/items, then persists and reloads it.
Character Build(string name, string cls, int level, (int s, int d, int c, int i, int w, int ch) ab,
                string? speciesKey, string? bgKey)
{
    var c = new Character
    {
        Name = name, CharacterClass = cls, CharacterLevel = level, Type = CombatantType.PC,
        Strength = ab.s, Dexterity = ab.d, Constitution = ab.c,
        Intelligence = ab.i, Wisdom = ab.w, Charisma = ab.ch,
        ProficiencyBonus = LevelUpRules.ProficiencyForLevel(level),
        MaxHitPoints = 8 + level * 6,
        IsBarbarianClass = cls == "Barbarian",
    };

    foreach (var (lvl, max) in LevelUpRules.SlotsForLevel(cls, level))
        c.SpellSlots.Add(new SpellSlot { CharacterId = c.Id, Level = lvl, MaxSlots = max });

    if (speciesKey != null) content.ApplySpecies(c, speciesKey);

    if (bgKey != null)
    {
        var bg = ContentLibrary.GetBackground(bgKey);
        var alloc = new List<AbilityBonus>();
        if (bg != null && bg.AbilityOptions.Count >= 2)
        {
            alloc.Add(new AbilityBonus(bg.AbilityOptions[0], 2));
            alloc.Add(new AbilityBonus(bg.AbilityOptions[1], 1));
        }
        content.ApplyBackground(c, bgKey, alloc);
    }

    // A subclass if one exists for this class at this level (mirrors picking one).
    var sub = ContentLibrary.SubclassesForClass(cls).FirstOrDefault();
    if (sub != null && level >= 3) content.ApplySubclass(c, sub.Key);

    // Player-owned content tagged distinctly so subclass swaps must NOT touch it.
    if (SpellLibrary.ClassCanCast(cls))
    {
        var pick = SpellLibrary.Search(null, cls, SpellLibrary.MaxSpellLevel(cls, level)).Take(2).ToList();
        foreach (var ls in pick)
        {
            var sp = SpellLibrary.ToSpell(ls, c.Id);
            sp.Source = "Player";
            c.Spells.Add(sp);
        }
    }
    c.Features.Add(new CharacterFeature { CharacterId = c.Id, Name = "Custom Note", Description = "player-added", Source = "Custom", LevelGained = 1 });
    c.Inventory.Add(new InventoryItem { CharacterId = c.Id, Name = "Trinket", Quantity = 1, Category = ItemCategory.Other });

    return svc.Create(c);
}

// ───────────────────────── 1. Build a 12-member party ─────────────────────────
Section("1. Build 12 characters across classes & levels");

var roster = new (string name, string cls, int level, (int, int, int, int, int, int) ab, string? sp, string? bg)[]
{
    ("Korran Vale",  "Barbarian", 4, (16,14,15,8,10,12),  "orc",        "soldier"),
    ("Thorin Vask",  "Fighter",   4, (16,13,15,10,11,9),  "dwarf",      "soldier"),
    ("Sister Ava",   "Cleric",    4, (10,12,14,11,16,13), "human",      "acolyte"),
    ("Lyra Emberez", "Sorcerer",  4, (8,14,13,11,10,16),  "tiefling",   "charlatan"),
    ("Finn Quick",   "Rogue",     4, (10,17,13,12,11,13), "halfling",   "criminal"),
    ("Elowen",       "Druid",     4, (10,13,14,12,16,11), "elf-wood",   "farmer"),
    ("Sir Roland",   "Paladin",   5, (16,10,14,9,11,15),  "human",      "soldier"),
    ("Melody Lin",   "Bard",      5, (9,14,13,12,11,16),  "halfling",   "entertainer"),
    ("Grok One-Eye", "Barbarian", 6, (17,13,16,8,11,10),  "goliath",    "soldier"),
    ("Zephyr",       "Wizard",    4, (8,14,13,16,12,10),  "elf-high",   "acolyte"),
    ("Hex Vlow",     "Warlock",   4, (9,13,14,11,10,16),  "tiefling",   "charlatan"),
    ("Sage Brookling","Ranger",   4, (12,16,13,11,14,10), "elf-wood",   "farmer"),
};

var built = new List<Character>();
foreach (var r in roster)
{
    try
    {
        var c = Build(r.name, r.cls, r.level, r.ab, r.sp, r.bg);
        var reloaded = svc.GetById(c.Id);
        Check(reloaded != null, $"{r.name}: persisted & reloadable");
        if (reloaded == null) continue;
        built.Add(reloaded);
        Check(reloaded.CharacterLevel == r.level, $"{r.name}: level persisted ({r.level})");
        Check(reloaded.Features.Any(f => f.Source == "Custom"), $"{r.name}: player feature survived save");
        Check(reloaded.Inventory.Any(i => i.Name == "Trinket"), $"{r.name}: inventory survived save");
        Check(reloaded.Skills.Count > 0, $"{r.name}: has skills after species/background");
        Note($"{r.name} [{r.cls} {r.level}] → spells:{reloaded.Spells.Count} feats:{reloaded.Features.Count} " +
             $"inv:{reloaded.Inventory.Count} skills:{reloaded.Skills.Count} slots:{reloaded.SpellSlots.Count} sub:'{reloaded.Subclass}'");
    }
    catch (Exception ex)
    {
        Check(false, $"{r.name}: EXCEPTION during build — {ex.GetType().Name}: {ex.Message}");
    }
}
Check(svc.GetAll().Count == roster.Length, $"GetAll returns all {roster.Length} characters");

// ───────────────────────── 2. Repeated-save stress (the old crash) ─────────────────────────
Section("2. Repeated save/reload stress (ReplaceCollection / DbContext path)");
{
    var c = built[0];
    try
    {
        for (int i = 0; i < 5; i++)
        {
            c.Spells.Add(new Spell { CharacterId = c.Id, Name = $"Probe Spell {i}", Level = 1, Source = "Player" });
            c.Features.Add(new CharacterFeature { CharacterId = c.Id, Name = $"Probe Feat {i}", Source = "Custom", LevelGained = 1 });
            svc.Update(c);
            c = svc.GetById(c.Id)!;          // reload between saves, as the UI does
        }
        Check(c.Spells.Count(s => s.Name.StartsWith("Probe Spell")) == 5, "5 consecutive saves each persisted a spell");
        Check(c.Features.Count(f => f.Name.StartsWith("Probe Feat")) == 5, "5 consecutive saves each persisted a feature");
        // Save twice with no changes in between (previously triggered concurrency errors).
        svc.Update(c); svc.Update(svc.GetById(c.Id)!);
        Check(true, "double save with no changes did not throw");
    }
    catch (Exception ex)
    {
        Check(false, $"repeated-save stress EXCEPTION — {ex.GetType().Name}: {ex.Message}");
    }
}

// ───────────────────────── 3. Level-up battery ─────────────────────────
Section("3. Level-up battery (HP, proficiency, slots, features persist & grow)");
foreach (var start in built.ToList())
{
    try
    {
        var c = svc.GetById(start.Id)!;
        int beforeLevel = c.CharacterLevel;
        int beforeHp = c.MaxHitPoints;
        int beforeFeats = c.Features.Count;
        var slotsBefore = c.SpellSlots.ToDictionary(s => s.Level, s => s.MaxSlots);

        c = LevelUp(c);   // +1 level

        Check(c.CharacterLevel == beforeLevel + 1, $"{c.Name}: level {beforeLevel}→{c.CharacterLevel}");
        Check(c.ProficiencyBonus == LevelUpRules.ProficiencyForLevel(c.CharacterLevel), $"{c.Name}: proficiency bonus correct ({c.ProficiencyBonus})");
        Check(c.MaxHitPoints > beforeHp, $"{c.Name}: HP increased ({beforeHp}→{c.MaxHitPoints})");
        Check(c.Features.Count >= beforeFeats, $"{c.Name}: feature count did not shrink on level-up");

        // Slots must match the class table for the new level and never decrease.
        foreach (var (lvl, max) in LevelUpRules.SlotsForLevel(c.CharacterClass, c.CharacterLevel))
        {
            var got = c.SpellSlots.FirstOrDefault(s => s.Level == lvl);
            Check(got != null && got.MaxSlots >= max, $"{c.Name}: L{lvl} slots ≥ table ({max})");
            if (slotsBefore.TryGetValue(lvl, out var prev))
                Check(got!.MaxSlots >= prev, $"{c.Name}: L{lvl} slots not reduced ({prev}→{got.MaxSlots})");
        }
        // No duplicate features by (name, source).
        var dupes = c.Features.GroupBy(f => (f.Name, f.Source)).Where(g => g.Count() > 1).Select(g => g.Key.Name).ToList();
        Check(dupes.Count == 0, $"{c.Name}: no duplicate features" + (dupes.Count > 0 ? $" (dupes: {string.Join(", ", dupes)})" : ""));
    }
    catch (Exception ex)
    {
        Check(false, $"{start.Name}: level-up EXCEPTION — {ex.GetType().Name}: {ex.Message}");
    }
}

// Verify the headline Korran scenario specifically: Barbarian 4→5 grants Extra Attack + Fast Movement.
Section("3b. Korran 4→5 headline scenario");
{
    var korran = svc.GetAll().First(c => c.Name == "Korran Vale");
    // already leveled once above (to 5). Check the features landed.
    Check(korran.CharacterLevel == 5, $"Korran is level {korran.CharacterLevel} (expected 5)");
    Check(korran.Features.Any(f => f.Name == "Extra Attack"), "Korran gained Extra Attack");
    Check(korran.Features.Any(f => f.Name == "Fast Movement"), "Korran gained Fast Movement");
}

// ───────────────────────── 4. ASI level + retroactive HP ─────────────────────────
Section("4. ASI level (Constitution bump → retroactive HP)");
{
    // Take a fighter from current level up to level 8 (an ASI level), bumping CON.
    var fighter = svc.GetAll().First(c => c.CharacterClass == "Fighter");
    while (fighter.CharacterLevel < 7) fighter = LevelUp(fighter);   // get to 7, next is 8 (ASI)
    int conBefore = fighter.Constitution;
    int hpBefore = fighter.MaxHitPoints;
    Check(LevelUpRules.IsAsiLevel("Fighter", 8), "level 8 is an ASI level for Fighter");
    fighter = LevelUp(fighter, "Constitution", "Constitution");   // +2 CON
    Check(fighter.Constitution == Math.Min(20, conBefore + 2), $"CON {conBefore}→{fighter.Constitution}");
    // If the +2 pushed CON across an even threshold, HP should jump by more than the base die gain.
    Check(fighter.MaxHitPoints > hpBefore, $"HP increased through ASI level ({hpBefore}→{fighter.MaxHitPoints})");
    Note($"Fighter now L{fighter.CharacterLevel}, CON {fighter.Constitution}, HP {fighter.MaxHitPoints}");
}

// ───────────────────────── 5. Subclass swap provenance ─────────────────────────
Section("5. Subclass swap (granted spells/features swap; player spells untouched)");
{
    // Use a caster with at least two subclasses available.
    var caster = svc.GetAll().First(c => c.CharacterClass == "Cleric");
    var subs = ContentLibrary.SubclassesForClass("Cleric").Where(s => s.GrantedSpells.Count > 0).Take(2).ToList();
    if (subs.Count < 2)
    {
        Note("fewer than 2 spell-granting Cleric subclasses; skipping swap test");
    }
    else
    {
        var (subA, subB) = (subs[0], subs[1]);

        content.ApplySubclass(caster, subA.Key);
        svc.Update(caster); caster = svc.GetById(caster.Id)!;
        int playerSpellsBefore = caster.Spells.Count(s => s.Source == "Player");
        Check(caster.Spells.Any(s => s.Source == subA.Name), $"subclass A ({subA.Name}) granted ≥1 spell");
        Check(caster.Features.Any(f => f.Source == subA.Name) || subA.Features.All(f => f.Level > caster.CharacterLevel),
              $"subclass A features applied (or none unlocked yet at L{caster.CharacterLevel})");

        content.ApplySubclass(caster, subB.Key);
        svc.Update(caster); caster = svc.GetById(caster.Id)!;
        Check(!caster.Spells.Any(s => s.Source == subA.Name), $"subclass A spells removed after swap to {subB.Name}");
        Check(!caster.Features.Any(f => f.Source == subA.Name), "subclass A features removed after swap");
        Check(caster.Spells.Any(s => s.Source == subB.Name), $"subclass B ({subB.Name}) granted ≥1 spell");
        Check(caster.Spells.Count(s => s.Source == "Player") == playerSpellsBefore,
              $"player-owned spells untouched by swap ({playerSpellsBefore})");
        Check(caster.Subclass == subB.Name, $"subclass field updated to {subB.Name}");
    }
}

// ───────────────────────── 6. Species/background reversal ─────────────────────────
Section("6. Species & background swap reverses cleanly (no orphan grants)");
{
    var c = svc.GetById(built[2].Id)!;   // Sister Ava (human/acolyte)
    var humanFeatCount = c.Features.Count;

    // Swap species human → dragonborn, then back to human.
    content.ApplySpecies(c, "dragonborn");
    Check(c.Race == "Dragonborn", "species changed to Dragonborn");
    content.ApplySpecies(c, "human");
    Check(c.Race == "Human", "species changed back to Human");
    Check(!c.Features.Any(f => f.Source == "Dragonborn"), "no leftover Dragonborn features after swapping back");

    // Swap background, ensure ability grants reverse.
    var bgBefore = c.AbilityGrants.Count;
    var strBefore = c.Strength + c.Dexterity + c.Constitution + c.Intelligence + c.Wisdom + c.Charisma;
    content.ApplyBackground(c, "criminal", new List<AbilityBonus> { new(AbilityScore.Dexterity, 2), new(AbilityScore.Intelligence, 1) });
    content.ApplyBackground(c, "acolyte", new List<AbilityBonus> { new(AbilityScore.Wisdom, 2), new(AbilityScore.Intelligence, 1) });
    var strAfter = c.Strength + c.Dexterity + c.Constitution + c.Intelligence + c.Wisdom + c.Charisma;
    Check(strAfter == strBefore, $"total ability points unchanged after background swap+back ({strBefore}→{strAfter})");
    Check(c.AbilityGrants.Count(g => g.Source.Contains("Criminal")) == 0, "criminal background ability grants removed");

    svc.Update(c);
    Check(svc.GetById(c.Id) != null, "re-saved cleanly after species/background churn");
}

// ───────────────────────── 7. Edge cases ─────────────────────────
Section("7. Edge cases");
{
    // Non-casters get no spell slots on level-up.
    var barb = svc.GetAll().First(c => c.CharacterClass == "Barbarian");
    Check(barb.SpellSlots.Count == 0, $"{barb.Name}: Barbarian has no spell slots");

    // Delete then confirm gone.
    var victim = Build("Disposable", "Monk", 3, (12,15,13,11,14,10), "human", "farmer");
    var vid = victim.Id;
    svc.Delete(vid);
    Check(svc.GetById(vid) == null, "deleted character is gone");
    Check(!svc.Exists(vid), "Exists() false after delete");

    // Level a caster all the way to 20 to stress the slot tables / no crash.
    try
    {
        var wiz = svc.GetAll().First(c => c.CharacterClass == "Wizard");
        while (wiz.CharacterLevel < 20) wiz = LevelUp(wiz);
        Check(wiz.CharacterLevel == 20, "wizard reached level 20 without error");
        Check(wiz.SpellSlots.Any(s => s.Level == 9 && s.MaxSlots >= 1), "level-20 wizard has a 9th-level slot");
    }
    catch (Exception ex)
    {
        Check(false, $"level-to-20 EXCEPTION — {ex.GetType().Name}: {ex.Message}");
    }
}

// ───────────────────────── 8. Live combat engine (shared multiplayer) ─────────────────────────
Section("8. Live combat engine — turn gating, manual controls, conditions, death saves");
{
    try
    {
        var engine = new CombatEngineService(svc);

        var hero = new Combatant
        {
            Id = Guid.NewGuid(), Name = "TestHero", Type = CombatantType.PC,
            MaxHitPoints = 30, CurrentHitPoints = 30, ArmorClass = 12, Dexterity = 16,
            Actions = { new CombatAction { Name = "Sword", ActionType = ActionType.Attack, AttackBonus = 12, DamageDice = "1d8", DamageBonus = 3, DamageType = DamageType.Slashing } },
            SpellSlots = { new SpellSlotState { Level = 1, Max = 3, Used = 0 } },
        };
        var goblin = new Combatant
        {
            Id = Guid.NewGuid(), Name = "Goblin", Type = CombatantType.Monster,
            MaxHitPoints = 200, CurrentHitPoints = 200, ArmorClass = 1, Dexterity = 8,
            Actions = { new CombatAction { Name = "Club", ActionType = ActionType.Attack, AttackBonus = 0, DamageDice = "1d4", DamageBonus = 0 } },
        };

        engine.AddCombatant(hero);
        engine.AddCombatant(goblin);
        engine.StartCombat();

        Check(engine.State == CombatState.Active, "combat starts Active with a monster present");
        Check(engine.CurrentCombatant != null, "there is a current combatant after StartCombat");
        Check(engine.SnapshotCombatants().All(c => c.ReactionAvailable), "everyone starts with their reaction");

        // Advance until it's the hero's turn (cap iterations).
        for (int i = 0; i < 10 && engine.CurrentCombatant?.Id != hero.Id; i++) engine.NextTurn();
        Check(engine.CurrentCombatant?.Id == hero.Id, "NextTurn advances to the hero's turn");

        // Gating: acting as someone whose turn it ISN'T is rejected.
        Check(!engine.PlayerAction(goblin.Id, hero.Actions[0], hero), "player action rejected when not your turn");

        // Hero acts on their turn → accepted, turn advances off the hero.
        var ok = engine.PlayerAction(hero.Id, hero.Actions[0], goblin);
        Check(ok, "player action accepted on your turn");
        Check(engine.CurrentCombatant?.Id != hero.Id, "turn advanced after the hero acted");
        Check(engine.SnapshotLog().Count > 0, "the action was recorded in the combat log");

        // Manual HP: damage then heal.
        engine.AdjustHp(hero.Id, -5);
        Check(hero.CurrentHitPoints == 25, "AdjustHp applies damage (30→25)");
        engine.AdjustHp(hero.Id, 3);
        Check(hero.CurrentHitPoints == 28, "AdjustHp heals (25→28)");

        // Conditions toggle on/off.
        engine.ToggleCondition(hero.Id, Condition.Poisoned);
        Check(hero.Conditions.Contains(Condition.Poisoned), "condition added");
        engine.ToggleCondition(hero.Id, Condition.Poisoned);
        Check(!hero.Conditions.Contains(Condition.Poisoned), "condition removed");

        // Live spell-slot tracking.
        engine.SetSlotUsed(hero.Id, 1, 2);
        Check(hero.SpellSlots.First(s => s.Level == 1).Used == 2, "spell slot marked used (live)");

        // Reaction spend.
        engine.UseReaction(goblin.Id);
        Check(!goblin.ReactionAvailable, "reaction marked spent");

        // Death saves: down the hero, roll one, expect a recorded result.
        engine.AdjustHp(hero.Id, -1000);
        Check(hero.IsUnconscious, "hero is unconscious at 0 HP");
        engine.RollDeathSaveFor(hero.Id);
        Check(hero.DeathSaveSuccesses + hero.DeathSaveFailures > 0 || hero.CurrentHitPoints > 0,
              "a death save was recorded (or a natural 20 revived)");

        engine.EndCombat();
        Check(engine.State == CombatState.Finished, "DM EndCombat finishes the encounter");
    }
    catch (Exception ex)
    {
        Check(false, $"live combat EXCEPTION — {ex.GetType().Name}: {ex.Message}");
    }
}

// ───────────────────────── report ─────────────────────────
Console.WriteLine($"\n────────────────────────────────────────");
Console.WriteLine($"Checks run : {checks}");
Console.WriteLine($"Passed     : {checks - failures.Count}");
Console.WriteLine($"Failed     : {failures.Count}");
if (failures.Count > 0)
{
    Console.WriteLine($"\nFAILURES:");
    foreach (var f in failures) Console.WriteLine($"  ✗ {f}");
}
else
{
    Console.WriteLine($"\n✅ All checks passed.");
}

try { File.Delete(dbPath); } catch { /* temp file */ }
Environment.Exit(failures.Count == 0 ? 0 : 1);

// ───────────────────────── support types ─────────────────────────
sealed class TestFactory(DbContextOptions<DndDbContext> opts) : IDbContextFactory<DndDbContext>
{
    public DndDbContext CreateDbContext() => new(opts);
}
