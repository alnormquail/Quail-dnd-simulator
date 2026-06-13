# Quail D&D Simulator — Project History & Reference

> A living document tracking what this app is, how it's built, and everything
> that's been done so far. Update it as the project grows.

**Repo:** https://github.com/alnormquail/Quail-dnd-simulator
**Owner:** alnormquail (Alex)
**Last updated:** 2026-06-12

> **Direction (2026-06-12):** The app's #1 priority is now a **character
> tracker/builder** (D&D-Beyond-style editing where picking options auto-applies
> their modifiers), with the full character-sheet reference and combat guide as
> the key views. The dice/combat simulator is secondary. Content leads with the
> **2024 PHB** (matches the party's sheets), tagged by source so 2014 +
> supplement content can layer in later. Builder supports both a from-scratch
> wizard and smart inline editing. Homebrew/custom content is deferred. See the
> "Character builder" roadmap and §6 changelog below.

### Character builder roadmap (phased)
1. **Engine + species** ✅ (Phase 1, done 2026-06-12) — content data model,
   provenance auto-apply, species picker.
2. **Backgrounds + Feats** ✅ (Phase 2, done 2026-06-12) — backgrounds with
   ability-score allocation + Origin feat; standalone feat picker; reversible
   ability-score grants.
3. **Class/subclass deepening** ✅ (Phase 3, done 2026-06-12) — subclass library,
   sheet picker, and level-up wizard integration (subclass features auto-grant
   at the right levels; wizard prompts for subclass at level 3).
4. **Creation wizard** ✅ (Phase 4, done 2026-06-12) — from-scratch flow
   (Basics → Abilities → Species → Background → Skills → Subclass → Review)
   with Standard Array / Point Buy / Manual ability generation.
5. **Items + content breadth** 🚧 (Phase 5 started 2026-06-12) — item/equipment
   library with auto-apply (weapons→attacks, armor/shield→AC) done; pouring in
   more books and filling Monk/Ranger/Warlock subclass depth is ongoing.

---

## 1. What this project is

A web app that acts as an **easier-to-use D&D Beyond** for Alex and his friends'
home game. Two big halves:

1. **Combat Simulator** — add heroes and monsters, roll initiative, run turns,
   track HP/conditions, and read a combat log. Monsters come from a built-in
   SRD library (works fully offline).
2. **Party Hub / Character management** — full character sheets with stats,
   spells, inventory, features, and notes; a printable one-page sheet; a
   player-facing quick-reference Guide; a guided **level-up wizard**; and a
   **phone preview** so you can see how any page looks on an iPhone.

Design goal: friendly enough for a **total beginner** to use, while still
following real D&D 5e (2024 / PHB-2024) rules.

---

## 2. Tech stack

| Layer | Choice |
|---|---|
| Framework | ASP.NET Core **Blazor Server** (.NET 10) |
| UI | Razor components (`.razor`) with `@rendermode InteractiveServer` |
| Data | **Entity Framework Core** + **SQLite** (`CopilotTest/dnd-party.db`) |
| Styling | Single global stylesheet `wwwroot/app.css` (CSS variables, dark green/red/gold theme) |
| Rules/content | Hard-coded C# data classes (no external API calls at runtime) |

**Important quirk:** the project folder/namespace is `CopilotTest` (it began as a
GitHub Copilot test project). The app lives in `Quail-dnd-simulator/CopilotTest/`.

### Running it locally
```bash
cd CopilotTest
dotnet run --urls "http://localhost:5179"
```
Then open http://localhost:5179. The SQLite DB and `.claude/` are **git-ignored**
— each player keeps their own local data.

---

## 3. Project structure (key files)

```
CopilotTest/
├── Program.cs                  # DI setup, DB creation, manual table migration, seeding
├── Data/DndDbContext.cs        # EF Core context + relationships
├── Components/
│   ├── MainLayout.razor        # Top nav (Combat / Characters / Guide / Phone)
│   ├── CombatantList.razor     # Combat roster + saved party roster
│   └── Pages/
│       ├── Home.razor          # "/" — combat simulator
│       ├── Characters.razor    # "/characters" — Party Hub grid + new-character form
│       ├── CharacterSheet.razor# "/characters/{id}" — full sheet, level-up wizard, spell picker
│       ├── CharacterGuide.razor# "/character-guide" — player quick reference
│       └── PhonePreview.razor   # "/phone-preview" — iPhone frame preview
├── Models/
│   ├── Character.cs            # Core PC/NPC model + computed modifiers
│   ├── Combatant.cs            # Runtime combat version of a character
│   ├── CharacterFeature.cs     # Class/species features on a sheet  (added 2026-06-12)
│   ├── Spell.cs / SpellSlot.cs / CharacterSkill.cs / InventoryItem.cs / CombatAction.cs
│   ├── LevelUpRules.cs         # Hit dice, prof bonus, ASI levels, slot tables, feature grants
│   ├── SpellLibrary.cs         # ~140 SRD spells (levels 0-5) for the picker  (added 2026-06-12)
│   ├── PreloadedCharacters.cs  # The seeded party (9 characters)
│   ├── Skills.cs               # Skill enum + ability map + display names
│   └── DndApiModels.cs         # Monster data shapes
└── Services/
    ├── CharacterService.cs     # CRUD over the DB; seeding; collection replacement
    ├── CombatEngineService.cs  # Turn order, attacks, damage, conditions, saved roster
    ├── DndApiService.cs        # Built-in offline SRD **monster** library
    └── PdfImportService.cs     # (D&D Beyond PDF import helper)
```

---

## 4. Data model notes

- **`Character`** holds ability scores, combat stats, save proficiencies (as
  bools), currency, and child collections: `Actions`, `Spells`, `Inventory`,
  `Skills`, `SpellSlots`, `Features`.
- Modifiers are **computed properties** (`StrengthModifier`, `GetSaveBonus`,
  `GetSkillBonus`, `PassivePerception`) — never stored.
- **`Character.ToCombatant()`** creates a fresh runtime `Combatant` for an
  encounter; `FromCombatant()` converts back when saving a new PC.
- Enums are stored **as strings** in SQLite (configured in `DndDbContext`) for
  readability.
- `CharacterService.Update()` saves scalar fields then **replaces each child
  collection wholesale** (delete-all + re-add) — simple and avoids EF tracking
  headaches, but means child row IDs change on every save.

### Database migration approach (important!)
The app uses `db.Database.EnsureCreated()`, which **creates the schema once and
never alters an existing database**. So when you add a new table/column, existing
players' databases won't get it automatically. The pattern used:
- New **tables** → add a `CREATE TABLE IF NOT EXISTS ...` block in `Program.cs`
  after `EnsureCreated()` (see the `CharacterFeatures` example). The raw SQL must
  match the EF model exactly.
- The **seeder** (`SeedIfEmpty`) is also idempotent: it adds any preloaded
  characters missing from the DB (not just when empty), and applies one-off data
  corrections.
> If the schema gets much more complex, consider switching to real EF Core
> migrations instead of `EnsureCreated` + hand-written SQL.

---

## 5. The party (PreloadedCharacters.cs)

All level 5 unless noted, seeded with deterministic GUIDs `a1000000-...-0000000N`:

| # | Name | Class | Race | Notes |
|---|------|-------|------|-------|
| 1 | Spurt the Sorcerer | Sorcerer 3 | Kobold | original demo char |
| 2 | Belqorel | Barbarian 5 | Dwarf | rage mechanics |
| 3 | Wally Cornbone | Rogue 4 | Halfling | expertise skills |
| 4 | Winnie Vale | Sorcerer 5 | High Elf | Wild Magic; full Guide entry |
| 5 | Kennyth | Paladin 5 | Gnome | full Guide entry |
| 6 | Boan Strickler | Fighter 5 | Gnome | from PDF |
| 7 | Gideon Silverspoon | Bard 5 | Half-Elf | from PDF; 3rd-level slots |
| 8 | Job Goodhammer | Paladin 5 | Human | from PDF |
| 9 | Bren Gunning | Druid 5 | Forest Gnome | from PDF; big prepared list |
| A | Korran Vale | Barbarian 4 | Half-Orc | from PDF; full Guide entry; a Vale, like Winnie |

Characters 6–9 were transcribed from D&D Beyond PDF character sheets in
`~/Downloads/ordercharacterslevel5pdfs/` (and `~/Downloads/Bren.pdf`).
**"Korran.pdf" turned out to be a duplicate of Kennyth's sheet** — there is no
separate Korran character.

The **Character Guide** page only shows characters that have a hand-written
`GuideData` entry (currently Winnie, Kennyth, Boan, Gideon, Job, Bren). Spurt,
Belqorel, and Wally appear in the Party Hub but not the Guide.

---

## 6. Feature timeline (what's been built)

### Initial state (commits `95f0cd3`, `d1974b8`)
- Combat simulator with offline SRD monsters, initiative, attacks, conditions.
- Party Hub + character sheets (stats / spells / inventory / profile tabs, edit mode).
- Character Guide quick-reference with a session tracker (HP, spell-slot pips,
  sorcery points) and Wild Magic info for Winnie; Kennyth panel added.

### Session 2026-06-12 (part 14) — Guide: "On Your Turn" action-economy

- New **⏱️ On Your Turn** section on the combat guide, splitting what a character
  can do into **Action / Bonus Action / Reaction / Movement & Free** columns.
- Auto-derived: weapon attacks (with to-hit/damage) go under Action; spells are
  bucketed by their casting time (`CastingBucket`) and enriched with save DC /
  attack / damage from a matching combat action. Universal reminders included
  (Opportunity Attack reaction, the standard actions, movement).
- Curated `TurnExtras` per character for class features with an action cost
  (Rage, Bardic Inspiration, Cutting Words, Lay on Hands, Second Wind/Action
  Surge, Wild Shape/Land's Aid, Innate Sorcery/Font of Magic, etc.).

### Session 2026-06-12 (part 13) — Add Korran Vale (Barbarian)

- The real Korran was finally located (the "Korran.pdf" in both order folders is
  a mislabeled copy of Kennyth's sheet; her actual sheet came via a separate
  PDF, `camillesharrow_159569379.pdf`).
- **Korran Vale** — Barbarian 4, Half-Orc (GUID …000a): STR 17/DEX 13/CON 15/
  INT 12/WIS 10/CHA 8, HP 41, AC 17 (Scale Mail of Force Resistance + Shield),
  Greataxe/Handaxe, proficient in Athletics/Intimidation/Perception/Performance/
  Persuasion/Survival, Rage 3/long rest, Relentless Endurance. Full inventory +
  Guide entry. (She shares the Vale surname with Winnie.)
- As a brand-new preloaded character she seeds fresh (full graph incl. inventory)
  on next startup — no backfill needed.

### Session 2026-06-12 (part 12) — Combat guide QoL: live skills/saves

- Replaced the guide's hand-typed "proficient skills/saves" tag strings with
  **computed** sections driven by the live character: full **Skills** list (all
  18, alphabetical, live modifiers, ●/◆ proficiency indicators, ability tags),
  all six **Saving Throws**, and **Passive Senses** (Perception/Investigation/
  Insight). Always accurate as characters change.
- Fixed the guide's Spell Save DC / attack to use the correct casting ability
  (was hardcoded to CHA — wrong for Bren the WIS-based Druid).
- (GuideData's old SavingThrows/ProficientSkills fields are now unused but left
  in place; harmless.)

### Session 2026-06-12 (part 11) — Load party inventory from the PDFs

- Extracted each PDF sheet's equipment list and added it to the templates
  (Winnie, Kennyth, Boan, Gideon, Job, Bren) via a new `Inv(...)` helper.
- `LoadPreloadedInventory()` loads it into existing DBs once, gated by the
  `preloaded-inventory-loaded-v1` AppMeta flag; only fills characters with an
  empty inventory, so user-added gear is never clobbered. Same pattern as the
  spell correction. (68 items loaded.)

### Session 2026-06-12 (part 10) — Starting equipment & magic-item hiding

Design discussion settled on: **curated by default, full access on demand**.
Magic items are DM loot, so they're kept out of the normal flow; starting gear
comes from the class + background (rules-defined), not a class×race×level matrix.
(Skipped: gold-by-level for higher starts, and curated flavor loadouts.)

- **Magic items hidden by default** in the sheet's "Add from Library" picker,
  behind a "Show magic items (DM loot)" toggle. The fix for the
  "+3 battleaxe on a level-2 wizard" problem.
- **`StartingKits`** (`Models/Content/StartingKits.cs`) — 2024 class kits as
  library refs (wire up attacks/AC) + plain gear (focuses, packs, spellbooks).
- **`ContentService.ApplyKitItem`** applies a kit line (library item or plain).
- **Creation wizard Equipment step** (now step 7, Review step 8): one-click
  "Add {Class} + {Background} starting equipment," a removable pending list, and
  an optional browse picker with magic items excluded. Applied on Create.

### Session 2026-06-12 (part 9) — Correct caster spell lists from the PDFs

- Re-read the level-5 PDF sheets and corrected each caster's spells to match
  exactly: Winnie (Prestidigitation/Minor Illusion/Friends + Shield/Sleep/
  Thunderwave + Hold Person/Aganazzar's), Kennyth (gnome/fighting-style cantrips
  + oath spells + Find Steed, no Cure Wounds), Job (lean: Spare the Dying/Sacred
  Flame/Shield of Faith/Find Steed), Bren (exact cantrips incl. Elementalism +
  prepared subset). Gideon's PDF had **no spells listed**, so sensible bard
  defaults were kept (flagged for the user).
- Added two missing cantrips to the library: **Friends** and **Elementalism**.
- The spell correction is a **one-time** fix: gated behind an `AppMeta` flag
  table (`preloaded-spells-corrected-v1`). It runs once to fix existing/old
  databases, then never again — so the party members can be hand-edited freely
  without their spells reverting. User-created characters are never touched.

### Session 2026-06-12 (part 8) — Populate preloaded casters' spell lists

- Each preloaded caster (Winnie, Kennyth, Gideon, Job, Bren; Spurt already had
  spells) now gets a `Spells` list built from the shared library by name via a
  new `PreloadedCharacters.SpellsFor(...)` helper — so descriptions stay DRY.
- `CharacterService.BackfillPreloadedSpells()` populates these into **existing**
  databases on startup (idempotent: only casters with an empty spell list, and
  it inserts Spell rows directly by FK to avoid the parent-row concurrency
  update that crashed the first attempt).

### Session 2026-06-12 (part 7) — Source-tagged spells from the party sheets

- Added a `Source` field to `LibrarySpell` (defaults to "PHB 2024", so all
  existing entries are untouched). This starts the source-tagging convention for
  layering in non-PHB / supplement / homebrew content later.
- Added the three party-sheet spells missing from the library:
  **Sorcerous Burst** (PHB 2024), **Aganazzar's Scorcher** (Elemental Evil),
  **Dragon's Breath** (Xanathar's Guide).
- Spell pickers (Spells tab + level-up step) now show a small source chip for any
  spell that isn't core PHB 2024, so non-by-the-book picks are clearly labeled.

### Session 2026-06-12 (part 6) — Character builder Phase 5 (Items & equipment)

**Content** (`Models/Content/ItemLibrary.cs`)
- `ItemData` + `ItemLibrary`: 2024 PHB simple/martial weapons, all armor (light/
  medium/heavy), shield, iconic magic items, and common gear.

**Engine** (`Services/ContentService.cs`)
- `AddItemFromLibrary` — weapons add an inventory item **and** a computed
  `CombatAction` (attack = ability mod + proficiency, ability auto-picked for
  finesse/ranged); armor/shields are equipped and trigger AC recompute.
- `RecalculateArmorClass` — 5e AC formula (light +DEX, medium +DEX cap 2, heavy
  flat, +2 shield, 10+DEX unarmored). Also called when toggling equip on the sheet.

**UI** (`CharacterSheet.razor`, Inventory tab)
- "📦 Add from Library" picker: search + type filter (Weapon/Armor/Shield/Magic/
  Gear), expandable details, one-click add. Equip-dot toggle now keeps AC in sync.

> Note: an `AddColumnIfMissing`-style migration was **not** needed here — items
> reuse the existing Inventory/Actions tables. AC is set at equip time (not a
> live formula), matching the bake-and-edit pattern used elsewhere.

### Session 2026-06-12 (part 5) — Character builder Phase 4 (Creation wizard)

**Content** (`Models/Content/`)
- `ClassLibrary` / `ClassData` — all 12 classes' saving-throw proficiencies and
  level-1 skill-choice lists/counts.
- `AbilityGen` — Standard Array values + Point Buy cost table/budget helpers.

**Page** (`Components/Pages/CharacterCreate.razor`, route `/create`)
- Step flow: Basics (name/class/level) → Abilities → Species → Background →
  Class Skills → Subclass (shown only at level 3+) → Review & Create.
- Ability scores: **Standard Array** (assign 15/14/13/12/10/8), **Point Buy**
  (27-point budget with live remaining count and +/- guards), or **Manual**.
- Reuses `ContentService.ApplySpecies/ApplyBackground/ApplySubclass`, so all
  picks auto-apply their effects. On Create it also sets class save proficiencies,
  chosen skills, class-appropriate HP (computed after background CON bonus), and
  spell slots, then opens the finished sheet.
- Party Hub: **🧙 Build a Character** opens the wizard; **+ Quick Add** keeps the
  old one-line form as a fallback.

### Session 2026-06-12 (part 4) — Character builder Phase 3 (Subclasses)

**Content** (`Models/Content/SubclassLibrary.cs`, `ContentModels.cs`)
- `SubclassData` + `SubclassFeature` (level-keyed). `SubclassLibrary` seeds all
  twelve classes' four 2024 PHB subclasses — every subclass has its defining
  level-3 feature; the party's classes (Barbarian, Bard, Cleric, Druid, Fighter,
  Paladin, Rogue, Sorcerer, Wizard) get features through levels 6/10/14+.
  Remaining feature depth for Monk/Ranger/Warlock is a fill-in-later content task.
- Exposed via `ContentLibrary.SubclassesForClass` / `GetSubclass`.

**Engine** (`Services/ContentService.cs`, `Character.SubclassKey` + migration)
- `ApplySubclass(character, key)` — grants all subclass features up to the
  character's current level, reversible by source tag (subclass name).
- `GrantSubclassFeaturesForLevel(character, level)` — used by the wizard to add
  exactly the features unlocked at a new level.

**UI** (`CharacterSheet.razor`)
- Profile tab: Subclass becomes a dropdown of the class's library subclasses
  (free-text fallback for unknown classes); picking applies features live.
- Level-up wizard: grants subclass features on level up, lists them in Review,
  and at level 3 (with no subclass yet) shows an inline "Choose your subclass!"
  prompt so the choice and its features happen during the level-up.

### Session 2026-06-12 (part 3) — Character builder Phase 2 (Backgrounds + Feats)

**Content** (`Models/Content/`)
- `BackgroundData` (16 2024 PHB backgrounds): three ability options the player
  allocates +2/+1 or +1/+1/+1, two skills, a tool, and an Origin feat.
- `FeatData` + `FeatCategory`: all 12 Origin feats plus popular General feats
  (Great Weapon Master, Sharpshooter, War Caster, Sentinel, Mobile, Resilient).
  Feats carry traits and optional fixed ability bumps; choice-based bumps
  (Tavern Brawler, Resilient) are described, not auto-applied, to avoid guessing.

**Reversible ability bonuses** (`Models/AbilityGrant.cs`)
- New `AbilityGrant` entity (Ability, Amount, Source) — a "receipt" for each
  bonus baked into a score, tagged by source so swapping a background/feat
  removes exactly its bonuses. New `AbilityGrants` table + `BackgroundKey`
  column installed via the existing safe-migration helpers in `Program.cs`.
- Wired through `CharacterService` (include + replace-collection).

**Engine** (`Services/ContentService.cs`)
- `ApplyBackground(character, key, abilityChoice)` — applies ability bonuses
  (per allocation), skill + tool proficiencies, and the granted Origin feat;
  reverses the previous background first (features, ability grants, skills all
  tagged by background name).
- `ApplyFeat` / `RemoveFeat` for standalone feats (tagged `"Feat: {name}"`),
  so removing a feat also removes its ability bonus.

**UI** (`CharacterSheet.razor`, Profile + Stats tabs, edit mode)
- Background dropdown + live ability-allocation controls (+2/+1 or +1/+1/+1,
  with ability pickers), showing granted skills/tool/feat.
- Standalone feat picker (Origin + General, grouped), with a removable chip
  list of current feats.
- Stats tab shows an "✨ Bonuses applied" provenance line listing each ability
  grant and its source.

> Known edge (documented): ability scores are stored as the live total (base +
> grants baked in). Manually editing a raw score while a grant is applied can
> desync removal math — acceptable at this scale; a true base/effective split
> can come later if needed.

### Session 2026-06-12 (part 2) — Character builder Phase 1

**Content engine foundation** (`Models/Content/`, `Services/ContentService.cs`)
- `ContentModels.cs` — edition-agnostic content records (`SpeciesData`,
  `ContentTrait`, `AbilityBonus`, `RulesEdition`); every entry tagged by `Source`
  + `Edition`.
- `ContentLibrary.cs` — read-only library, seeded with **~14 2024 PHB species**
  (Human, Elf high/wood/drow, Dwarf, Gnome forest/rock, Halfling, Orc,
  Dragonborn, Tiefling, Aasimar, Goliath). 2024 species grant no ability bonuses
  (those live on backgrounds — Phase 2).
- `ContentService.ApplySpecies()` — provenance auto-apply: picking a species
  writes its traits (into `CharacterFeatures`, `Source` = species name), skill
  proficiencies, speed, and Race onto the sheet; swapping/clearing removes
  exactly what the old species added (library is source of truth, no per-char
  delta storage).
- `Character.SpeciesKey` added; column installed on existing DBs via a new
  `AddColumnIfMissing` helper in `Program.cs` (SQLite has no
  `ADD COLUMN IF NOT EXISTS`, so it checks `pragma_table_info` first).
- Species dropdown wired into the sheet's Profile tab (edit mode): pick a species
  → traits/proficiencies apply live and show on the Stats tab; "Custom" option
  keeps the free-text Race field for anything not in the library.

> Decision reversal: briefly chose 2014 as the content spine, then switched to
> **2024** because the party's sheets are 2024 and the rules are more uniform
> (cleaner auto-apply). The one already-written file was edition-agnostic, so no
> rework.

### Session 2026-06-12 (part 1) (commits `5b398cf` → `16e4574`)

**Roster & data**
- Added Boan, Gideon, Job, Bren from PDFs (9 characters total).
- Corrected Winnie's stats to match her sheet (HP 32, DEX 13, INT 14, and her
  metamagic is **Twinned Spell**, not Heightened).
- Made the seeder add missing characters to existing databases.

**Printable & mobile**
- One-page **printable character sheet** (`@media print`, `.ps-*` classes).
- Mobile-responsive CSS (`@media (max-width: 600px)`), nav wraps on phones.
- **📱 Phone Preview** page — pick any page + iPhone model (SE / 15 / Pro Max),
  renders live in an iPhone frame via an `<iframe>`.

**Level-up wizard** (`CharacterSheet.razor` + `LevelUpRules.cs`)
- Guided multi-step modal: **HP → (Ability Scores) → (Spells) → Review & Apply**.
- HP uses the **average** method (half hit die + 1 + CON mod), editable if the DM rolls.
- Auto-updates proficiency bonus and spell slots from real 5e class tables
  (full casters + half casters).
- **ASI step** at the right levels (4/8/12/16/19, plus Fighter 6/14, Rogue 10),
  with +2-to-one or +1-to-two and a max-20 guard; raising CON retroactively adds HP.
- Logs each level-up to the character's Notes with the date.

**Spell library & picker** (`SpellLibrary.cs`)
- Built-in offline library of **~140 SRD spells, levels 0–5**, tagged by class.
- Used in two places: the wizard's **Spells step**, and an **"📚 Add from
  Library"** button on the Spells tab (works outside edit mode). Search by name,
  filter by level, expand to read full text, one-click add. Marks already-known spells.

**Auto-granted features** (`CharacterFeature.cs` + `LevelUpRules.FeatureGrants`)
- Leveling up now **adds class features to the sheet** with full rules text
  (e.g. Paladin 6 → Aura of Protection, Fighter 5 → Extra Attack + Tactical
  Shift, Rogue 5 → Uncanny Dodge + Cunning Strike). Covers the party's classes.
- New **Features & Traits** section on the Stats tab (editable; also printed).

**Inventory quality-of-life**
- Equip toggle, quantity **−/+** buttons (removes at 0), and **+ Quick Add** —
  all without entering edit mode; they save immediately.

**Other QoL**
- Spells tab shows **spellcasting ability, Spell Save DC, and spell attack bonus**.
- New characters get **class-appropriate starting HP and spell slots** (no more flat 10 HP).
- Corrected the Home page subtitle (app is offline, not API-powered).
- Stopped tracking `dnd-party.db*` and `.claude/` in git.

---

## 7. Known limitations / honest caveats

- **No shared state between players.** Each person's data lives in their own
  local SQLite file. Leveling up on one machine doesn't sync to others. To get a
  shared party, host the app on one machine everyone connects to over wifi
  (or move to a hosted DB).
- **Subclass features** are surfaced as reminders, not auto-added (they vary too
  much per character). Base-class features are auto-granted.
- The collection-replacement save strategy regenerates child row IDs on each
  save — fine for this app's scale, but noted in case it matters later.
- Spell library covers **levels 0–5** and SRD spells only. Higher levels /
  non-SRD spells need to be added by hand or extended in `SpellLibrary.cs`.
- Level-up wizard click-flow is interactive Blazor; it's been smoke-tested
  (pages load, render clean) but not fully click-tested end to end by automation.

---

## 8. Ideas / backlog (not yet built)

- Shared/hosted party state so the whole table sees the same data.
- Spell levels 6–9 and class spell-preparation limits in the wizard.
- Subclass feature auto-grant (per-subclass data tables).
- Short-rest / long-rest button that restores slots, HP, and limited-use features.
- Dice roller integrated into the sheet (attacks, saves, skill checks).
- Import directly from a D&D Beyond PDF (the `PdfImportService` is a starting point).

---

## 9. Environment & git notes (shared Mac)

This MacBook is **shared by two GitHub accounts**: `alnormquail` (Alex) and
`emnemnemie` (Emily). Git auth is set up so they don't clobber each other:
- Alex's `alnormquail` token is the **host-level** Keychain entry for github.com.
- Emily's repos (`~/kitchen-brain`, `~/family-calendar-app`) use repo-local
  `credential.useHttpPath true`, so her `emnemnemie` token is stored scoped to
  those paths. **Don't remove these path-scoped entries or that config.**
- If a push fails with "Repository not found", check which account is active:
  `printf "protocol=https\nhost=github.com\n" | git credential fill`
  and re-enter the correct token.
- Never embed tokens in remote URLs. Never re-add a git remote to the stale
  `~/family-calendar` folder (the live project is `~/family-calendar-app`).

---

*End of history. Keep this current — add a dated entry under §6 whenever you ship
something.*
