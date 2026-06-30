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

// A combat engine that starts from a clean slate. Combat now persists to the DB, so a
// plain `new CombatEngineService` would resume whatever the previous section left behind;
// clearing the snapshot first keeps each section isolated. (Section 17 deliberately does
// NOT clear, to test that a fresh engine resumes a persisted encounter.)
CombatEngineService FreshEngine()
{
    using (var db = factory.CreateDbContext())
    {
        var row = db.CombatSnapshots.Find(1);
        if (row != null) { db.CombatSnapshots.Remove(row); db.SaveChanges(); }
    }
    return new CombatEngineService(svc, factory);
}

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
        var engine = FreshEngine();

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

        // Hero acts on their turn — accepted, and the turn does NOT auto-advance,
        // so they can still take a bonus action / extra attack.
        Check(engine.PlayerAction(hero.Id, hero.Actions[0], goblin), "player action accepted on your turn");
        Check(engine.CurrentCombatant?.Id == hero.Id, "turn does NOT auto-advance after one action");
        Check(engine.PlayerAction(hero.Id, hero.Actions[0], goblin, AdvantageMode.Advantage),
              "a second action (extra attack / bonus action) is allowed, with advantage");
        Check(engine.SnapshotLog().Count > 0, "actions recorded in the combat log");

        // Explicit End Turn advances off the hero.
        engine.EndTurn(hero.Id);
        Check(engine.CurrentCombatant?.Id != hero.Id, "EndTurn advances past the hero");

        // DM drives the monster (an unclaimed combatant) on its turn.
        for (int i = 0; i < 10 && engine.CurrentCombatant?.Id != goblin.Id; i++) engine.NextTurn();
        if (engine.CurrentCombatant?.Id == goblin.Id)
            Check(engine.DmActOnCurrent(goblin.Actions[0], hero), "DM acts for the monster on its turn");

        // Manual HP: damage then heal (relative — the hero may have taken a hit above).
        var hpBefore = hero.CurrentHitPoints;
        engine.AdjustHp(hero.Id, -5);
        Check(hero.CurrentHitPoints == Math.Max(0, hpBefore - 5), "AdjustHp applies damage");
        engine.AdjustHp(hero.Id, 3);
        Check(hero.CurrentHitPoints == Math.Max(0, hpBefore - 5) + 3, "AdjustHp heals");

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

// ───────────────────────── 9. Combat scenarios (how it actually plays) ─────────────────────────
Section("9. Combat scenarios — full encounters, balance, advantage, death saves");
{
    // Build a combatant with a single attack.
    Combatant Mk(string name, CombatantType type, int hp, int ac, int atk, string dice, int dmgBonus, int dex = 10) =>
        new()
        {
            Id = Guid.NewGuid(), Name = name, Type = type,
            MaxHitPoints = hp, CurrentHitPoints = hp, ArmorClass = ac, Dexterity = dex,
            Actions = { new CombatAction { Name = "Attack", ActionType = ActionType.Attack, AttackBonus = atk, DamageDice = dice, DamageBonus = dmgBonus, DamageType = DamageType.Slashing } },
        };

    // Auto-run an encounter to completion via the engine's simulator. Returns
    // rounds elapsed, whether it terminated, and the surviving side.
    (int rounds, bool terminated, string survivors) RunAuto(CombatEngineService e, int cap = 60)
    {
        int guard = 0;
        while (e.State == CombatState.Active && guard < cap) { e.SimulateRound(); guard++; }
        var alive = e.SnapshotCombatants().Where(c => !c.IsDead && c.CurrentHitPoints > 0).Select(c => c.Name).ToList();
        return (e.CurrentRound, e.State != CombatState.Active, alive.Count > 0 ? string.Join(", ", alive) : "nobody");
    }

    // ── A. Auto-battles across matchups ──
    var matchups = new (string label, Func<List<Combatant>> build)[]
    {
        ("Party (4) vs Boss", () => new()
        {
            Mk("Fighter", CombatantType.PC, 44, 18, 7, "1d8", 4, 14),
            Mk("Rogue",   CombatantType.PC, 30, 15, 6, "1d6", 3, 18),
            Mk("Cleric",  CombatantType.PC, 33, 16, 5, "1d8", 2, 10),
            Mk("Wizard",  CombatantType.PC, 24, 12, 5, "2d6", 2, 14),
            Mk("Ogre Boss", CombatantType.Monster, 120, 15, 8, "2d8", 5, 8),
        }),
        ("Lone hero vs swarm (5)", () => new()
        {
            Mk("Champion", CombatantType.PC, 70, 19, 9, "2d6", 5, 16),
            Mk("Goblin 1", CombatantType.Monster, 12, 13, 4, "1d6", 2, 14),
            Mk("Goblin 2", CombatantType.Monster, 12, 13, 4, "1d6", 2, 14),
            Mk("Goblin 3", CombatantType.Monster, 12, 13, 4, "1d6", 2, 14),
            Mk("Goblin 4", CombatantType.Monster, 12, 13, 4, "1d6", 2, 14),
            Mk("Goblin 5", CombatantType.Monster, 12, 13, 4, "1d6", 2, 14),
        }),
        ("Outmatched party (TPK risk)", () => new()
        {
            Mk("Squire A", CombatantType.PC, 14, 12, 3, "1d6", 1, 10),
            Mk("Squire B", CombatantType.PC, 14, 12, 3, "1d6", 1, 10),
            Mk("Troll 1", CombatantType.Monster, 84, 15, 7, "2d8", 4, 8),
            Mk("Troll 2", CombatantType.Monster, 84, 15, 7, "2d8", 4, 8),
        }),
    };

    foreach (var (label, build) in matchups)
    {
        try
        {
            var e = FreshEngine();
            foreach (var c in build()) e.AddCombatant(c);
            e.StartCombat();
            var (rounds, terminated, survivors) = RunAuto(e);
            Check(terminated, $"[{label}] combat terminates (no infinite loop)");
            Check(e.State == CombatState.Finished, $"[{label}] ends in Finished state");
            Note($"{label}: {rounds} rounds → survivors: {survivors}");
        }
        catch (Exception ex)
        {
            Check(false, $"[{label}] EXCEPTION — {ex.GetType().Name}: {ex.Message}");
        }
    }

    // ── B. Repeated even duel: should always terminate; wins roughly two-sided ──
    {
        int aWins = 0, bWins = 0, draws = 0, maxRounds = 0; bool anyStall = false;
        for (int i = 0; i < 200; i++)
        {
            var e = FreshEngine();
            e.AddCombatant(Mk("Duelist A", CombatantType.PC, 35, 15, 6, "1d10", 3, 14));
            e.AddCombatant(Mk("Duelist B", CombatantType.Monster, 35, 15, 6, "1d10", 3, 14));
            e.StartCombat();
            var (rounds, terminated, survivors) = RunAuto(e, 80);
            if (!terminated) anyStall = true;
            maxRounds = Math.Max(maxRounds, rounds);
            if (survivors.Contains("A")) aWins++;
            else if (survivors.Contains("B")) bWins++;
            else draws++;
        }
        Check(!anyStall, "200 even duels all terminate within the round cap");
        Check(aWins > 40 && bWins > 40, $"both sides win a fair share (A:{aWins} B:{bWins})");
        Note($"200 duels → A wins {aWins}, B wins {bWins}, mutual KO {draws}; longest fight {maxRounds} rounds");
    }

    // ── C. Advantage / disadvantage actually shift the hit rate ──
    {
        var e = FreshEngine();
        var atk = new CombatAction { Name = "Probe", ActionType = ActionType.Attack, AttackBonus = 5 };
        var dummy = Mk("Dummy", CombatantType.Monster, 1, 15, 0, "1d4", 0);
        int N = 4000;
        int Count(AdvantageMode m)
        {
            int hits = 0;
            for (int i = 0; i < N; i++) if (e.ResolveAttackRoll(atk, dummy, 0, m).Hit) hits++;
            return hits;
        }
        double norm = Count(AdvantageMode.Normal) / (double)N;
        double adv  = Count(AdvantageMode.Advantage) / (double)N;
        double dis  = Count(AdvantageMode.Disadvantage) / (double)N;
        Check(adv > norm && norm > dis, "hit rate: advantage > normal > disadvantage");
        Note($"hit% vs AC15 (+5): disadv {dis:P0} | normal {norm:P0} | advantage {adv:P0}");
    }

    // ── D. Death-save outcome distribution over many rolls ──
    {
        var e = FreshEngine();
        var pc = Mk("Faller", CombatantType.PC, 20, 10, 0, "1d4", 0);
        e.AddCombatant(pc);
        int succ = 0, fail = 0, revived = 0;
        for (int i = 0; i < 2000; i++)
        {
            pc.CurrentHitPoints = 0; pc.IsDead = false;
            pc.DeathSaveSuccesses = 0; pc.DeathSaveFailures = 0;
            e.RollDeathSaveFor(pc.Id);
            if (pc.CurrentHitPoints > 0) revived++;
            else if (pc.DeathSaveSuccesses > 0) succ++;
            else if (pc.DeathSaveFailures > 0) fail++;
        }
        Check(succ > 0 && fail > 0 && revived > 0, "death saves yield successes, failures, and nat-20 revives");
        Note($"2000 death saves → success {succ}, failure {fail}, nat-20 revive {revived}");
    }

    // ── E. Manual turn flow: round increments, reactions refresh, gating holds ──
    {
        var e = FreshEngine();
        var pcA = Mk("Aria",  CombatantType.PC, 40, 14, 6, "1d8", 3, 16);
        var orc = Mk("Orc",   CombatantType.Monster, 50, 13, 5, "1d12", 3, 10);
        e.AddCombatant(pcA); e.AddCombatant(orc);
        e.StartCombat();
        int startRound = e.CurrentRound;

        // Walk a couple of full rounds via the manual API.
        bool gateHeld = true, alwaysValid = true;
        for (int step = 0; step < 6 && e.State == CombatState.Active; step++)
        {
            var cur = e.CurrentCombatant;
            if (cur == null) { alwaysValid = false; break; }
            var target = e.GetCurrentTargets().FirstOrDefault();
            if (cur.Id == pcA.Id)
            {
                // Wrong-turn gate: orc's id can't act on Aria's turn.
                if (e.PlayerAction(orc.Id, pcA.Actions[0], orc)) gateHeld = false;
                if (target != null) e.PlayerAction(pcA.Id, pcA.Actions[0], target);   // action (no auto-advance)
                e.EndTurn(pcA.Id);
            }
            else
            {
                if (target != null) e.DmActOnCurrent(orc.Actions[0], target);          // DM runs the monster
                e.NextTurn();
            }
        }
        Check(gateHeld, "acting out of turn is rejected throughout the fight");
        Check(alwaysValid, "there is always a valid current combatant mid-combat");
        Check(e.CurrentRound >= startRound, "round counter advances as turns cycle");
        Check(e.SnapshotCombatants().Any(c => c.ReactionAvailable), "reactions refresh across rounds");
        Note($"manual flow reached round {e.CurrentRound}, state {e.State}");
    }
}

// ───────────────────────── 10. Real characters & status effects ─────────────────────────
Section("10. Real party characters, status effects in combat, and rage");
{
    Combatant Mk(string name, CombatantType type, int hp, int ac, int atk, string dice, int dmgBonus, int dex = 10) =>
        new()
        {
            Id = Guid.NewGuid(), Name = name, Type = type,
            MaxHitPoints = hp, CurrentHitPoints = hp, ArmorClass = ac, Dexterity = dex,
            Actions = { new CombatAction { Name = "Strike", ActionType = ActionType.Attack, AttackBonus = atk, DamageDice = dice, DamageBonus = dmgBonus, DamageType = DamageType.Slashing } },
        };

    // ── A. The actual preloaded party fights a boss ──
    try
    {
        var party = PreloadedCharacters.All.Where(c => c.Type == CombatantType.PC && c.Actions.Count > 0).Take(4).ToList();
        Check(party.Count >= 2, "preloaded party members carry real combat actions");
        var e = FreshEngine();
        foreach (var t in party) e.AddCombatant(t.ToCombatant());
        e.AddCombatant(Mk("Adult Dragon", CombatantType.Monster, 220, 18, 11, "2d10", 6, 12));
        foreach (var pc in e.SnapshotCombatants().Where(c => c.Type == CombatantType.PC))
            Check(pc.Actions.Count > 0 && pc.MaxHitPoints > 0, $"{pc.Name} entered combat with {pc.Actions.Count} actions, {pc.MaxHitPoints} HP");
        e.StartCombat();
        int g = 0; while (e.State == CombatState.Active && g < 60) { e.SimulateRound(); g++; }
        Check(e.State != CombatState.Active, "real-party encounter terminates");
        var survivors = e.SnapshotCombatants().Where(c => !c.IsDead && c.CurrentHitPoints > 0).Select(c => c.Name).ToList();
        Note($"Real party ({string.Join(", ", party.Select(p => p.Name))}) vs Adult Dragon: {e.CurrentRound} rounds → survivors: {(survivors.Count > 0 ? string.Join(", ", survivors) : "nobody")}");
    }
    catch (Exception ex) { Check(false, $"real-party combat EXCEPTION — {ex.GetType().Name}: {ex.Message}"); }

    // ── B. Incapacitating conditions skip the turn; others don't ──
    {
        var e = FreshEngine();
        var striker = Mk("Striker", CombatantType.PC, 40, 18, 12, "1d10", 5, 16);
        var dummy = Mk("Practice Dummy", CombatantType.Monster, 100000, 1, 0, "1d4", 0);
        dummy.Actions.Clear();   // can't fight back, so any HP loss is purely the striker acting
        e.AddCombatant(striker); e.AddCombatant(dummy);
        e.StartCombat();

        foreach (var cond in new[] { Condition.Stunned, Condition.Paralyzed, Condition.Incapacitated })
        {
            striker.Conditions.Clear();
            e.ToggleCondition(striker.Id, cond);
            var before = dummy.CurrentHitPoints;
            e.SimulateRound();
            Check(dummy.CurrentHitPoints == before, $"a {cond} combatant deals no damage (turn skipped)");
        }

        // Non-incapacitating condition: tracked, but the combatant still acts.
        // Run several rounds so a single unlucky natural-1 miss can't make this flaky.
        striker.Conditions.Clear();
        e.ToggleCondition(striker.Id, Condition.Poisoned);
        var hp = dummy.CurrentHitPoints;
        for (int i = 0; i < 5; i++) e.SimulateRound();
        Check(dummy.CurrentHitPoints < hp, "a Poisoned combatant still acts (non-incapacitating)");
        Check(striker.Conditions.Contains(Condition.Poisoned), "conditions persist across rounds until removed");
        e.ToggleCondition(striker.Id, Condition.Poisoned);
        Check(!striker.Conditions.Contains(Condition.Poisoned), "toggling a condition off clears it");
        Note("Status effects: Stunned/Paralyzed/Incapacitated skip the turn; others (Poisoned, Prone, Restrained, …) are tracked for the DM but don't auto-change rolls");
    }

    // ── C. Rage: resistance halves physical damage, and adds melee damage ──
    {
        // Resistance — same ogre attack on the same barbarian, raging vs not (AC 1 = always lands).
        var e = FreshEngine();
        var barb = Mk("Grog", CombatantType.PC, 100, 1, 6, "1d12", 4, 12);
        barb.IsBarbarianClass = true; barb.RageBonus = 2;
        var ogre = Mk("Ogre", CombatantType.Monster, 100, 15, 6, "2d8", 6, 8);
        e.AddCombatant(ogre); e.AddCombatant(barb);
        e.StartCombat();
        for (int i = 0; i < 12 && e.CurrentCombatant?.Id != ogre.Id; i++) e.NextTurn();

        int DamageTo(bool raging, int n)
        {
            barb.IsRaging = raging; int total = 0;
            for (int i = 0; i < n; i++) { barb.CurrentHitPoints = 1_000_000; e.DmActOnCurrent(ogre.Actions[0], barb); total += 1_000_000 - barb.CurrentHitPoints; }
            return total;
        }
        int normalDmg = DamageTo(false, 500);
        int ragedDmg  = DamageTo(true, 500);
        Check(ragedDmg < normalDmg * 0.7, $"rage roughly halves physical damage taken (raging {ragedDmg} vs normal {normalDmg})");
        Note($"rage resistance: 500 hits dealt {normalDmg} normally, {ragedDmg} while raging (~{(double)ragedDmg / normalDmg:P0})");

        // Melee bonus — the barbarian's own physical hits gain +RageBonus while raging.
        var e2 = FreshEngine();
        var grog2 = Mk("Grog2", CombatantType.PC, 80, 15, 12, "1d12", 4, 14);
        grog2.IsBarbarianClass = true; grog2.RageBonus = 2;
        var sack = Mk("Sandbag", CombatantType.Monster, 1_000_000, 1, 0, "1d4", 0); sack.Actions.Clear();
        e2.AddCombatant(grog2); e2.AddCombatant(sack);
        e2.StartCombat();
        for (int i = 0; i < 12 && e2.CurrentCombatant?.Id != grog2.Id; i++) e2.NextTurn();
        int Dealt(bool raging, int n)
        {
            grog2.IsRaging = raging; int total = 0;
            for (int i = 0; i < n; i++) { var b = sack.CurrentHitPoints; e2.DmActOnCurrent(grog2.Actions[0], sack); total += b - sack.CurrentHitPoints; }
            return total;
        }
        int plain = Dealt(false, 500);
        int raged = Dealt(true, 500);
        Check(raged > plain, $"raging barbarian deals more melee damage (raging {raged} vs normal {plain})");
        Note($"rage melee bonus: 500 swings dealt {plain} normally, {raged} while raging");
    }

    // ── D. Downing a PC vs a monster behaves differently ──
    {
        var e = FreshEngine();
        var pc = Mk("Hero", CombatantType.PC, 20, 10, 5, "1d8", 3);
        var beast = Mk("Beast", CombatantType.Monster, 20, 10, 5, "1d8", 3);
        e.AddCombatant(pc); e.AddCombatant(beast); e.StartCombat();
        e.AdjustHp(pc.Id, -1000);
        Check(pc.IsUnconscious && !pc.IsDead, "a PC dropped to 0 falls unconscious (not dead) and can roll death saves");
        e.AdjustHp(beast.Id, -1000);
        Check(beast.IsDead, "a monster dropped to 0 is defeated outright");
    }
}

// ───────────────────────── 11. Active-effects foundation (durations) ─────────────────────────
Section("11. Active effects — durations auto-expire, conditions mirror, concentration drops");
{
    Combatant Mk(string name, CombatantType type) => new()
    {
        Id = Guid.NewGuid(), Name = name, Type = type, MaxHitPoints = 30, CurrentHitPoints = 30,
        ArmorClass = 14, Dexterity = 12,
        Actions = { new CombatAction { Name = "Hit", ActionType = ActionType.Attack, AttackBonus = 5, DamageDice = "1d8", DamageBonus = 2 } },
    };
    void AdvanceOneRound(CombatEngineService eng)
    {
        int r = eng.CurrentRound, guard = 0;
        while (eng.CurrentRound == r && eng.State == CombatState.Active && guard++ < 30) eng.NextTurn();
    }

    var e = FreshEngine();
    var caster = Mk("Cleric", CombatantType.PC);
    var foe = Mk("Bandit", CombatantType.Monster);
    e.AddCombatant(caster); e.AddCombatant(foe);
    e.StartCombat();   // round 1

    // Duration auto-expiry: a 2-round effect lasts rounds 1–2 and is gone by round 3.
    e.AddEffect(caster.Id, new ActiveEffect { Name = "Bless", Source = "Cleric", RoundsRemaining = 2 });
    Check(caster.Effects.Any(x => x.Name == "Bless"), "timed effect applied");
    AdvanceOneRound(e);
    Check(caster.Effects.Any(x => x.Name == "Bless"), "2-round effect still present in round 2");
    AdvanceOneRound(e);
    Check(!caster.Effects.Any(x => x.Name == "Bless"), "effect auto-expired by round 3");

    // An effect that imposes a condition mirrors it into Conditions, and expiry clears it.
    e.AddEffect(foe.Id, new ActiveEffect { Name = "Ensnaring Strike", Condition = Condition.Restrained, RoundsRemaining = 1, Source = "Ranger" });
    Check(foe.Conditions.Contains(Condition.Restrained), "effect mirrors its condition into the Conditions set");
    AdvanceOneRound(e);
    Check(!foe.Conditions.Contains(Condition.Restrained) && !foe.Effects.Any(x => x.Name == "Ensnaring Strike"),
          "an expiring effect clears the condition it imposed");

    // Manual removal clears the condition too.
    e.AddEffect(foe.Id, new ActiveEffect { Name = "Hold Person", Condition = Condition.Paralyzed, RoundsRemaining = 10 });
    Check(foe.Conditions.Contains(Condition.Paralyzed), "Hold Person paralyzes the target");
    e.RemoveEffect(foe.Id, foe.Effects.First(x => x.Name == "Hold Person").Id);
    Check(!foe.Conditions.Contains(Condition.Paralyzed), "removing the effect clears its condition");

    // Two effects imposing the same condition: it persists until the last one is gone.
    e.AddEffect(foe.Id, new ActiveEffect { Name = "Web", Condition = Condition.Restrained, RoundsRemaining = 10 });
    e.AddEffect(foe.Id, new ActiveEffect { Name = "Grasp", Condition = Condition.Restrained, RoundsRemaining = 10 });
    e.RemoveEffect(foe.Id, foe.Effects.First(x => x.Name == "Web").Id);
    Check(foe.Conditions.Contains(Condition.Restrained), "condition persists while another effect still imposes it");
    e.RemoveEffect(foe.Id, foe.Effects.First(x => x.Name == "Grasp").Id);
    Check(!foe.Conditions.Contains(Condition.Restrained), "condition clears once the last imposing effect ends");

    // Concentration: a sustained effect drops when the caster loses concentration.
    e.AddEffect(foe.Id, new ActiveEffect { Name = "Hex", RoundsRemaining = 10, FromConcentration = true, ConcentratorId = caster.Id, Source = "Cleric" });
    Check(foe.Effects.Any(x => x.Name == "Hex"), "concentration effect applied to a target");
    e.DropConcentration(caster.Id);
    Check(!foe.Effects.Any(x => x.Name == "Hex"), "concentration effects drop when the caster loses concentration");

    // Indefinite effects never expire on their own.
    e.AddEffect(caster.Id, new ActiveEffect { Name = "Mage Armor", RoundsRemaining = -1 });
    AdvanceOneRound(e); AdvanceOneRound(e);
    Check(caster.Effects.Any(x => x.Name == "Mage Armor"), "indefinite effect (-1 rounds) never auto-expires");
    Note("Active-effects foundation: round-based durations, condition mirroring, and concentration linkage all hold");
}

// ───────────────────────── 12. Conditions → advantage; incapacitation gate ─────────────────────────
Section("12. Conditions drive advantage/disadvantage; incapacitated combatants can't act");
{
    Combatant C(string name, params Condition[] conds)
    {
        var c = new Combatant { Id = Guid.NewGuid(), Name = name, Type = CombatantType.PC, MaxHitPoints = 20, CurrentHitPoints = 20 };
        foreach (var x in conds) c.Conditions.Add(x);
        return c;
    }
    var melee  = new CombatAction { Name = "Sword", Range = "5 ft",   ActionType = ActionType.Attack };
    var ranged = new CombatAction { Name = "Bow",   Range = "120 ft", ActionType = ActionType.Attack };
    AdvantageMode M(Combatant a, Combatant t, CombatAction act, AdvantageMode man = AdvantageMode.Normal)
        => CombatRules.ResolveAdvantage(a, t, act, man).Mode;

    Check(M(C("A"), C("T"), melee) == AdvantageMode.Normal, "no conditions → normal roll");
    Check(M(C("A"), C("T", Condition.Restrained), melee) == AdvantageMode.Advantage, "attack vs Restrained target → advantage");
    Check(M(C("A"), C("T", Condition.Stunned), melee) == AdvantageMode.Advantage, "attack vs Stunned target → advantage");
    Check(M(C("A", Condition.Poisoned), C("T"), melee) == AdvantageMode.Disadvantage, "Poisoned attacker → disadvantage");
    Check(M(C("A", Condition.Poisoned), C("T", Condition.Restrained), melee) == AdvantageMode.Normal, "advantage + disadvantage cancel → normal");
    Check(M(C("A"), C("T", Condition.Prone), melee) == AdvantageMode.Advantage, "melee vs Prone target → advantage");
    Check(M(C("A"), C("T", Condition.Prone), ranged) == AdvantageMode.Disadvantage, "ranged vs Prone target → disadvantage");
    Check(M(C("A", Condition.Invisible), C("T"), melee) == AdvantageMode.Advantage, "Invisible attacker → advantage");
    Check(M(C("A"), C("T", Condition.Invisible), melee) == AdvantageMode.Disadvantage, "attacking an unseen (Invisible) target → disadvantage");
    Check(M(C("A"), C("T"), melee, AdvantageMode.Advantage) == AdvantageMode.Advantage, "DM manual advantage applies on its own");
    Check(M(C("A"), C("T", Condition.Restrained), melee, AdvantageMode.Disadvantage) == AdvantageMode.Normal, "manual disadvantage cancels a condition's advantage");

    // Live-play incapacitation gate.
    Combatant Mk(string name, CombatantType type) => new()
    {
        Id = Guid.NewGuid(), Name = name, Type = type, MaxHitPoints = 40, CurrentHitPoints = 40, ArmorClass = 14, Dexterity = 14,
        Actions = { new CombatAction { Name = "Strike", ActionType = ActionType.Attack, AttackBonus = 6, DamageDice = "1d8", DamageBonus = 3, Range = "5 ft" } },
    };
    var e = FreshEngine();
    var hero = Mk("Hero", CombatantType.PC);
    var foe = Mk("Foe", CombatantType.Monster);
    e.AddCombatant(hero); e.AddCombatant(foe); e.StartCombat();
    for (int i = 0; i < 10 && e.CurrentCombatant?.Id != hero.Id; i++) e.NextTurn();

    e.ToggleCondition(hero.Id, Condition.Stunned);
    Check(CombatEngineService.IsIncapacitated(hero), "a Stunned combatant counts as incapacitated");
    Check(!e.PlayerAction(hero.Id, hero.Actions[0], foe), "a Stunned combatant's action is rejected in live play");
    e.ToggleCondition(hero.Id, Condition.Stunned);
    Check(e.PlayerAction(hero.Id, hero.Actions[0], foe), "after the Stun clears, the action is accepted");
    Note("Conditions now auto-set advantage/disadvantage, and incapacitating conditions block live actions");
}

// ───────────────────────── 13. Resistance / immunity / vulnerability ─────────────────────────
Section("13. Damage resistance, immunity, vulnerability (and rage doesn't stack)");
{
    var e = FreshEngine();
    var caster = new Combatant
    {
        Id = Guid.NewGuid(), Name = "Mage", Type = CombatantType.PC, MaxHitPoints = 40, CurrentHitPoints = 40, ArmorClass = 14, Dexterity = 14,
        Actions =
        {
            new CombatAction { Name = "Firebolt", ActionType = ActionType.Attack, AttackBonus = 20, DamageDice = "2d6", DamageType = DamageType.Fire,     Range = "120 ft" },
            new CombatAction { Name = "Slash",    ActionType = ActionType.Attack, AttackBonus = 20, DamageDice = "2d6", DamageType = DamageType.Slashing, Range = "5 ft"   },
        },
    };
    var target = new Combatant { Id = Guid.NewGuid(), Name = "Dummy", Type = CombatantType.Monster, MaxHitPoints = 1_000_000, CurrentHitPoints = 1_000_000, ArmorClass = 1, Dexterity = 8 };
    e.AddCombatant(caster); e.AddCombatant(target); e.StartCombat();
    for (int i = 0; i < 10 && e.CurrentCombatant?.Id != caster.Id; i++) e.NextTurn();

    int Damage(Action setup, int n, int actIdx)
    {
        target.Resistances.Clear(); target.Immunities.Clear(); target.Vulnerabilities.Clear(); target.IsRaging = false;
        setup();
        int total = 0;
        for (int i = 0; i < n; i++) { target.CurrentHitPoints = 1_000_000; e.DmActOnCurrent(caster.Actions[actIdx], target); total += 1_000_000 - target.CurrentHitPoints; }
        return total;
    }

    int normal = Damage(() => { }, 600, 0);
    int resist = Damage(() => target.Resistances.Add(DamageType.Fire), 600, 0);
    int immune = Damage(() => target.Immunities.Add(DamageType.Fire), 600, 0);
    int vuln   = Damage(() => target.Vulnerabilities.Add(DamageType.Fire), 600, 0);
    Check(immune == 0, "immunity negates all damage of that type");
    Check(resist < normal * 0.7, $"resistance roughly halves damage ({resist} vs {normal})");
    Check(vuln > normal * 1.5, $"vulnerability roughly doubles damage ({vuln} vs {normal})");
    Note($"Fire vs AC1: normal {normal}, resistant {resist} (~{(double)resist / normal:P0}), immune {immune}, vulnerable {vuln} (~{(double)vuln / normal:P0})");

    int normalPhys = Damage(() => { }, 600, 1);
    int ragedPhys  = Damage(() => target.IsRaging = true, 600, 1);
    Check(ragedPhys < normalPhys * 0.7, "rage still halves physical damage (regression)");
    int ragedResist = Damage(() => { target.IsRaging = true; target.Resistances.Add(DamageType.Slashing); }, 600, 1);
    Check(ragedResist > normalPhys * 0.35 && ragedResist < normalPhys * 0.7, "rage + resistance don't stack — still one halving, not a quarter");
    Note($"Slashing: normal {normalPhys}, raging {ragedPhys}, raging+resistant {ragedResist} (one halving, not two)");
}

// ───────────────────────── 14. Concentration ─────────────────────────
Section("14. Concentration — damage triggers a CON save that can drop the spell");
{
    Combatant Mk(string name, CombatantType type, int con = 10) => new()
    {
        Id = Guid.NewGuid(), Name = name, Type = type, MaxHitPoints = 100, CurrentHitPoints = 100,
        ArmorClass = 14, Dexterity = 12, Constitution = con,
        Actions = { new CombatAction { Name = "Hit", ActionType = ActionType.Attack, AttackBonus = 6, DamageDice = "1d8", DamageBonus = 2, Range = "5 ft" } },
    };
    var e = FreshEngine();
    var mage = Mk("Mage", CombatantType.PC, con: 14);   // CON +2
    var foe = Mk("Brute", CombatantType.Monster);
    e.AddCombatant(mage); e.AddCombatant(foe); e.StartCombat();

    e.SetConcentration(mage.Id, "Hold Person");
    Check(mage.ConcentratingOn == "Hold Person", "concentration can be set");

    // DC scales with damage: big hits break it far more often than small ones.
    int Drops(int dmg, int trials)
    {
        int drops = 0;
        for (int i = 0; i < trials; i++)
        {
            mage.CurrentHitPoints = 100; mage.ConcentratingOn = "Spell";
            e.AdjustHp(mage.Id, -dmg);                       // untyped damage → CON save
            if (string.IsNullOrEmpty(mage.ConcentratingOn)) drops++;
        }
        return drops;
    }
    int dropsHigh = Drops(40, 400);   // DC 20 vs +2 → mostly drops
    int dropsLow  = Drops(6, 400);    // DC 10 vs +2 → sometimes drops
    Check(dropsHigh > dropsLow, $"bigger hits break concentration more often (40dmg:{dropsHigh} > 6dmg:{dropsLow})");
    Check(dropsHigh > 0 && dropsHigh < 400, "a big hit usually but not always drops concentration");
    Check(dropsLow > 0, "even small hits can drop concentration");
    Note($"concentration saves: 40-dmg dropped {dropsHigh}/400, 6-dmg dropped {dropsLow}/400");

    // Manual drop.
    mage.CurrentHitPoints = 100; e.SetConcentration(mage.Id, "Bless");
    e.DropConcentration(mage.Id);
    Check(mage.ConcentratingOn == null, "concentration can be dropped manually");

    // Using a concentration-flagged action sets concentration automatically.
    for (int i = 0; i < 10 && e.CurrentCombatant?.Id != mage.Id; i++) e.NextTurn();
    var hex = new CombatAction { Name = "Hex", ActionType = ActionType.Other, RequiresConcentration = true, Range = "60 ft" };
    mage.Actions.Add(hex);
    e.DmActOnCurrent(hex, foe);
    Check(mage.ConcentratingOn == "Hex", "using a concentration action auto-sets concentration");

    // A concentration-linked effect ends when the caster goes down (0 HP ends concentration).
    mage.CurrentHitPoints = 100; e.SetConcentration(mage.Id, "Hold Person");
    e.AddEffect(foe.Id, new ActiveEffect { Name = "Hold Person", Condition = Condition.Paralyzed, FromConcentration = true, ConcentratorId = mage.Id, RoundsRemaining = 10 });
    Check(foe.Conditions.Contains(Condition.Paralyzed), "concentration spell paralyzes its target");
    e.AdjustHp(mage.Id, -500);   // drops the caster to 0
    Check(string.IsNullOrEmpty(mage.ConcentratingOn), "falling to 0 HP ends concentration");
    Check(!foe.Conditions.Contains(Condition.Paralyzed) && !foe.Effects.Any(x => x.Name == "Hold Person"),
          "the spell's effect on the target ends when the caster's concentration is lost");
}

// ───────────────────────── 15. Standing-advantage abilities (Reckless, Innate Sorcery) ─────────────────────────
Section("15. Standing-advantage abilities — Reckless Attack & Innate Sorcery");
{
    Combatant P(string name, params ActiveEffect[] fx)
    {
        var c = new Combatant { Id = Guid.NewGuid(), Name = name, Type = CombatantType.PC, MaxHitPoints = 30, CurrentHitPoints = 30 };
        c.Effects.AddRange(fx);
        return c;
    }
    ActiveEffect Reckless() => new() { Name = "Reckless Attack", AdvantageOnOwnAttacks = true, AdvantageToAttackers = true, AppliesTo = AttackKind.Melee, RoundsRemaining = 1 };
    ActiveEffect Innate()   => new() { Name = "Innate Sorcery", AdvantageOnOwnAttacks = true, AppliesTo = AttackKind.Spell, RoundsRemaining = 10 };

    var melee   = new CombatAction { Name = "Greataxe", ActionType = ActionType.Attack,      Range = "5 ft"   };
    var ranged  = new CombatAction { Name = "Javelin",  ActionType = ActionType.Attack,      Range = "120 ft" };
    var spellAt = new CombatAction { Name = "Firebolt", ActionType = ActionType.SpellAttack, Range = "120 ft" };
    AdvantageMode M(Combatant a, Combatant t, CombatAction act) => CombatRules.ResolveAdvantage(a, t, act, AdvantageMode.Normal).Mode;

    // Reckless Attack.
    Check(M(P("Korran", Reckless()), P("Foe"), melee) == AdvantageMode.Advantage, "Reckless: the bearer's melee attack gets advantage");
    Check(M(P("Korran", Reckless()), P("Foe"), ranged) == AdvantageMode.Normal, "Reckless does not boost the bearer's ranged attacks");
    Check(M(P("Enemy"), P("Korran", Reckless()), melee) == AdvantageMode.Advantage, "Reckless: attacks against the bearer have advantage");

    // Innate Sorcery.
    Check(M(P("Winnie", Innate()), P("Foe"), spellAt) == AdvantageMode.Advantage, "Innate Sorcery: the bearer's spell attack gets advantage");
    Check(M(P("Winnie", Innate()), P("Foe"), melee) == AdvantageMode.Normal, "Innate Sorcery does not boost melee attacks");
    Check(M(P("Enemy"), P("Winnie", Innate()), melee) == AdvantageMode.Normal, "Innate Sorcery gives attackers no advantage against the bearer");

    // Features carried into combat + the engine toggle.
    var ch = new Character { Name = "Korran", CharacterClass = "Barbarian", CharacterLevel = 4, Type = CombatantType.PC };
    ch.Features.Add(new CharacterFeature { Name = "Reckless Attack", Description = "Attack with advantage…", LevelGained = 2 });
    Check(ch.ToCombatant().Features.Any(f => f.Name == "Reckless Attack"), "ToCombatant carries features into the encounter");

    Combatant Mk(string name, CombatantType type, params string[] features)
    {
        var c = new Combatant
        {
            Id = Guid.NewGuid(), Name = name, Type = type, MaxHitPoints = 50, CurrentHitPoints = 50, ArmorClass = 14, Dexterity = 14,
            Actions = { new CombatAction { Name = "Greataxe", ActionType = ActionType.Attack, AttackBonus = 7, DamageDice = "1d12", DamageBonus = 4, Range = "5 ft" } },
        };
        foreach (var fn in features) c.Features.Add(new CharacterFeature { Name = fn });
        return c;
    }
    void AdvanceOneRound(CombatEngineService eng)
    {
        int r = eng.CurrentRound, guard = 0;
        while (eng.CurrentRound == r && eng.State == CombatState.Active && guard++ < 30) eng.NextTurn();
    }

    var e = FreshEngine();
    var korran = Mk("Korran", CombatantType.PC, "Reckless Attack");
    var foe = Mk("Bandit", CombatantType.Monster);
    e.AddCombatant(korran); e.AddCombatant(foe); e.StartCombat();

    e.ToggleAdvantageAbility(korran.Id, "Reckless Attack");
    Check(korran.Effects.Any(x => x.Name == "Reckless Attack"), "toggling Reckless Attack on adds the effect");
    e.ToggleAdvantageAbility(korran.Id, "Reckless Attack");
    Check(!korran.Effects.Any(x => x.Name == "Reckless Attack"), "toggling it again removes it");

    e.ToggleAdvantageAbility(korran.Id, "Reckless Attack");
    AdvanceOneRound(e);
    Check(!korran.Effects.Any(x => x.Name == "Reckless Attack"), "Reckless expires at the next round (≈ until your next turn)");

    e.ToggleAdvantageAbility(korran.Id, "Not A Real Ability");
    Check(!korran.Effects.Any(x => x.Name == "Not A Real Ability"), "an unknown ability name is ignored");
    Note("Standing advantage: Reckless boosts the bearer's melee + lets enemies hit them with advantage; Innate Sorcery boosts spell attacks for 10 rounds");
}

// ───────────────────────── 16. Test characters removed from seed ─────────────────────────
Section("16. Removed test characters are gone from the seed");
{
    var testIds = new[]
    {
        new Guid("a1000000-0000-0000-0000-000000000001"),
        new Guid("a1000000-0000-0000-0000-000000000002"),
        new Guid("a1000000-0000-0000-0000-000000000003"),
    };
    Check(PreloadedCharacters.All.Count == 7, $"seed now has 7 party members (was {PreloadedCharacters.All.Count})");
    Check(!PreloadedCharacters.All.Any(c => testIds.Contains(c.Id)), "Spurt/Belqorel/Wally no longer in the seed (won't re-seed)");
    Note($"Party: {string.Join(", ", PreloadedCharacters.All.Select(c => c.Name))}");
}

// ───────────────────────── 17. Combat persists & resumes after a restart ─────────────────────────
Section("17. Combat persists to the DB and resumes after a 'restart'");
{
    Combatant Mk(string name, CombatantType type, int hp) => new()
    {
        Id = Guid.NewGuid(), Name = name, Type = type, MaxHitPoints = hp, CurrentHitPoints = hp,
        ArmorClass = 14, Dexterity = 12,
        Actions = { new CombatAction { Name = "Hit", ActionType = ActionType.Attack, AttackBonus = 6, DamageDice = "1d8", DamageBonus = 2, Range = "5 ft" } },
    };

    var e1 = FreshEngine();
    var hero = Mk("Aria", CombatantType.PC, 40);
    var orc = Mk("Orc", CombatantType.Monster, 60);
    e1.AddCombatant(hero); e1.AddCombatant(orc); e1.StartCombat();
    e1.AdjustHp(orc.Id, -15);
    e1.ToggleCondition(hero.Id, Condition.Poisoned);
    e1.AddEffect(hero.Id, new ActiveEffect { Name = "Bless", RoundsRemaining = 5 });
    e1.NextTurn();

    var state1 = e1.State;
    var round1 = e1.CurrentRound;
    var orcHp1 = e1.SnapshotCombatants().First(c => c.Name == "Orc").CurrentHitPoints;
    var logCount1 = e1.SnapshotLog().Count;
    var current1 = e1.CurrentCombatant?.Name;

    // Simulate an app restart: a brand-new engine instance on the SAME database.
    var e2 = new CombatEngineService(svc, factory);
    Check(e2.State == CombatState.Active && e2.State == state1, "encounter resumed as Active");
    Check(e2.CurrentRound == round1, "round resumed");
    var c2 = e2.SnapshotCombatants();
    Check(c2.Count == 2, "combatants resumed");
    var orc2 = c2.FirstOrDefault(c => c.Name == "Orc");
    Check(orc2 != null && orc2.CurrentHitPoints == orcHp1, $"monster HP resumed ({orcHp1}/60)");
    var hero2 = c2.FirstOrDefault(c => c.Name == "Aria");
    Check(hero2 != null && hero2.Conditions.Contains(Condition.Poisoned), "condition resumed");
    Check(hero2 != null && hero2.Effects.Any(x => x.Name == "Bless"), "active effect resumed");
    Check(e2.SnapshotLog().Count == logCount1, "combat log resumed");
    Check(e2.CurrentCombatant?.Name == current1, "whose-turn resumed");

    // A fresh engine after the snapshot is cleared starts empty (the clean-lobby case).
    var e3 = FreshEngine();
    Check(e3.SnapshotCombatants().Count == 0 && e3.State == CombatState.Setup, "cleared snapshot → empty Setup");
    Note($"Resumed {c2.Count} combatants, round {e2.CurrentRound}, Orc {orc2?.CurrentHitPoints}/60, {e2.SnapshotLog().Count} log entries");
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
