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
3. **Class/subclass deepening** — subclass features into the level-up wizard.
4. **Creation wizard** — from-scratch flow (Species → Class → Background →
   Abilities → Equipment) built on phases 1–3.
5. **Items + content breadth** — magic items; pour in more books, source-tagged.

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
