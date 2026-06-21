# Quail D&D Simulator — Session Handoff

> Written at the end of the 2026-06-12 mega-session (Alex's session).
> Use this to pick up exactly where we left off.

---

## Goal

Build an **easy-to-use D&D character tracker/builder** for a home game — closer
to D&D Beyond than a bare spreadsheet. The combat dice simulator is secondary.

**Priority order:**
1. Character tracker/builder — intuitive editing where picking options auto-applies their effects (species, background, subclass, feats, spells, items)
2. Full character sheet reference — polished, per-character, with a printable version
3. Combat guide — per-player quick reference with click-to-roll dice
4. Dice simulator — nice-to-have

**Rules:** 2024 PHB as the spine. All content source-tagged so 2014/supplement content can layer in later. No homebrew (except Oathbreaker, which was explicitly added this session).

---

## Current State

The app is **fully functional and running** on the LAN profile. Everything listed below is in the database and GitHub.

### What works right now
- **Character builder** — creation wizard (8 steps: Basics → Abilities → Species → Background → Skills → Subclass → Equipment → Review), species/background/subclass/feat pickers all auto-apply their effects and reverse cleanly on swap
- **Level-up wizard** — guided HP/ASI/spells/subclass flow, auto-grants class features and subclass spells on level-up
- **Spell provenance** — spells tagged by source (subclass/background/feat); switching subclasses swaps only that subclass's granted spells, never touching player-added spells
- **Item library** — 2024 PHB weapons/armor + broad DMG magic-item set; magic items hidden by default behind a "Show magic items (DM loot)" toggle
- **Combat guide** — per-character Guide tab on every sheet (not just the standalone Guide page); "On Your Turn" action-economy breakdown with full mechanics and click-to-roll dice; live computed skills/saves/passives; spell descriptions
- **LAN play** — `dotnet run --launch-profile lan` serves on `http://0.0.0.0:5179`; players reach it at `http://10.0.0.248:5179` on the same wifi

### Party (10 characters, all in DB)
| GUID suffix | Name | Class | Level |
|---|---|---|---|
| 001 | Spurt the Sorcerer | Sorcerer | 3 |
| 002 | Belqorel | Barbarian | 5 |
| 003 | Wally Cornbone | Rogue | 4 |
| 004 | Winnie Vale | Sorcerer | 5 |
| 005 | Kennyth | Paladin | 5 |
| 006 | Boan Strickler | Fighter | 5 |
| 007 | Gideon Silverspoon | Bard | 5 |
| 008 | Job Goodhammer | Paladin | 5 |
| 009 | Bren Gunning | Druid | 5 |
| 00a | Korran Vale | Barbarian | 4 |

All have: spells, inventory, features, subclass linked. Korran is **still level 4** — the level-up bug was fixed this session but she hasn't been leveled yet.

### Subclass spell grants (20 subclasses now auto-grant spells on selection)
- **Paladin**: Devotion, Glory, Ancients, Vengeance, Open Sea (Spelljammer), **Oathbreaker (Homebrew 2024-adapted)**
- **Cleric**: Life, Light, Trickery, War
- **Warlock**: Archfey, Celestial, Fiend, Great Old One
- **Sorcerer**: Aberrant, Clockwork, Draconic
- **Druid**: Circle of the Land, Circle of the Sea
- **Ranger**: Fey Wanderer, Gloom Stalker

---

## Files In Flight / Touched This Session

### Modified (key changes)
| File | What changed |
|---|---|
| `Services/CharacterService.cs` | **Complete rewrite** — switched from `AddDbContext` (one context/circuit = stale state) to `IDbContextFactory` (fresh context per operation). `Update()` now loads fresh, replaces collections, assigns new IDs. |
| `Program.cs` | `AddDbContext` → `AddDbContextFactory`; startup block gets context from factory |
| `Models/Spell.cs` | Added `Source` field (provenance tag) |
| `Models/Content/SubclassLibrary.cs` | Added `GrantedSpells` to 20 subclasses; added Oathbreaker |
| `Models/Content/ContentModels.cs` | Added `GrantedSpells` to `SubclassData` |
| `Models/SpellLibrary.cs` | Added Arms of Hadar, Hunger of Hadar, Sending, Hellish Rebuke, Friends, Elementalism |
| `Models/Content/ItemLibrary.cs` | Added ~55 DMG magic items via `Mi()` helper |
| `Models/PreloadedCharacters.cs` | Added SubclassKey/Subclass to all party members; Features lists; Korran added |
| `Services/ContentService.cs` | `ApplySubclass` now grants/removes spells by source; `GrantSubclassSpells`, `RefreshSubclassSpells` added |
| `Components/GuidePanel.razor` | Extracted from CharacterGuide as a reusable component; added TurnItems with full mechanics and click-to-roll; inject DiceService/RollLogService |
| `Components/Pages/CharacterGuide.razor` | Now a thin wrapper around `<GuidePanel>` |
| `Components/Pages/CharacterSheet.razor` | Added Guide tab; level-up wizard calls RefreshSubclassSpells; subclass dropdown now populated |

### New files this session
- `Models/Content/ClassLibrary.cs` — class save proficiencies + skill choices
- `Models/Content/AbilityGen.cs` — Standard Array / Point Buy helpers
- `Models/Content/StartingKits.cs` — 2024 class starting-equipment kits
- `Models/Content/Spellcasting.cs` — cantrips-known/spells-prepared per class/level
- `Models/CharacterFeature.cs` — CharacterFeature entity
- `Models/AbilityGrant.cs` — AbilityGrant entity (reversible ability bonuses)
- `Models/Content/SubclassLibrary.cs` — all 12 classes' subclasses
- `Models/Content/ContentLibrary.cs` — central index (species/backgrounds/feats/subclasses)
- `Components/GuidePanel.razor` — extracted reusable guide component
- `Components/Pages/CharacterCreate.razor` — 8-step creation wizard

---

## Failed Attempts / What Didn't Work

### 1. `DbContext` per Blazor circuit (the real root cause of Korran's level-up)
- **Attempt 1:** `_db.SaveChanges()` directly on the tracked entity → `UNIQUE constraint failed: CharacterFeatures.Id`
  - Why: `ReplaceCollection` deletes rows then re-adds the same in-memory objects — EF tries to INSERT rows that already have IDs matching what was just deleted but are still tracked.
- **Attempt 2:** Check `_db.Entry(character).State != Detached` and skip to `SaveChanges()` → `DbUpdateConcurrencyException: expected to affect 1 row, affected 0`
  - Why: The previous failed save left the context in a broken state. Subsequent saves on the same circuit's context kept failing even after the earlier exception.
- **Fix (current):** `IDbContextFactory` — every operation gets a brand-new context, so stale state from previous failures can never carry over.

### 2. `Guid.NewGuid()` assigned to int-keyed entities
- `CharacterSkill.Id` and `SpellSlot.Id` are `int` (SQLite auto-increment), not `Guid`.
- Setting `s.Id = Guid.NewGuid()` in the new `ReplaceCollection` call → compile error CS0029.
- **Fix:** Set `s.Id = 0` for those two; SQLite assigns a new int on INSERT.

---

## Next Step

**Test Korran's level-up end-to-end on a phone.**

The fix is pushed and the app is running on `http://10.0.0.248:5179`. Open Korran's sheet, tap ⬆️ Level Up, step through the wizard (HP → Subclass choice at level 5 since she has none set → Review), and hit Apply. She should land at Barbarian 5 with Extra Attack and Fast Movement added to her Features tab.

If it succeeds: commit a quick test note and move on to whatever's next.
If it fails: run `tail -30 /tmp/dnd-app.log` and paste the error — the new architecture means any failure will be a clean, new error message rather than cascading stale-state noise.

---

## How to Run

```bash
# Normal (localhost only)
cd ~/Quail-dnd-simulator/CopilotTest
dotnet run --urls "http://localhost:5179"

# For game night (LAN access for phones)
dotnet run --launch-profile lan
# → players open http://10.0.0.248:5179 (check IP with: ipconfig getifaddr en0)
```

GitHub: https://github.com/alnormquail/Quail-dnd-simulator (owner: alnormquail)
Current branch: master, latest commit: `335d8a5`
