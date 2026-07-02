# Quail D&D Simulator — Handoff

> Living handoff for picking the project back up (or briefing a fresh chat).
> Last updated: 2026-06-28.

## What it is
A D&D (2024 PHB) character tracker + **live, shared multiplayer combat tracker** for a home
game. A DM runs an encounter from one device; players join from their phones, see combat
update in real time, and take their turns. Built on the original character-sheet/combat-sim
app; the major recent work is the live multiplayer layer plus a lightweight rules engine.

## Where it lives
- **Repo:** `github.com/alnormquail/Quail-dnd-simulator` (owner: Alex; Emily also works on it).
  Local clone: `~/Quail-dnd-simulator`. Main branch: `master` (everything pushed).
- **Main project:** `CopilotTest/` (the app). Headless test harness: `QuailTests/`.
- **Live site:** **https://quail-party.duckdns.org/play** — behind a shared password gate,
  username **`party`** (the password is set in the server's Caddyfile and is **not** stored in
  this repo — ask Emily/Alex).
- **Server:** DigitalOcean droplet, Ubuntu 24.04, IP `198.199.79.46`. Runs as systemd service
  **`quail`** from `/opt/quail`, bound to localhost; **Caddy** reverse-proxies with automatic
  HTTPS + the password gate. Full deploy guide: `HOSTING.md`.

## Tech stack
Blazor **Server** (.NET 10), EF Core + **SQLite** (`dnd-party.db`, seeded from
`Models/PreloadedCharacters.cs`). No external APIs; no per-user auth (the Caddy shared password
is the only gate).

## Key architecture
- **`Services/CombatEngineService.cs`** — the combat engine, a **singleton** so every player's
  browser circuit shares one encounter. All mutators funnel through `NotifyChanged()` (updates
  the UI via `OnStateChanged` **and** persists). Lock-guarded; the UI reads via
  `SnapshotCombatants()` / `SnapshotLog()`.
- **Seats** — `SeatRegistry` (singleton: who is DM / which PC is claimed) + `PlayerSeat`
  (per-browser). Lobby at `/play` to claim DM / a character / spectator. Seats persist in
  browser localStorage and auto-restore on reload; claims are reclaimable.
- **Rules engine** (`Models/CombatRules.cs` + engine): conditions auto-apply advantage/
  disadvantage; damage resistance/immunity/vulnerability; concentration (damage → CON save);
  an **active-effects system** (timed effects that expire by round, incl. concentration-linked).
  Standing-advantage abilities (Reckless Attack, Innate Sorcery) auto-detected from features.
- **`Components/TurnPanel.razor`** — the player's turn interface: guide-style Action/Bonus/
  Reaction columns wired to the shared engine (pick target → action → live roll/damage/advantage
  synced to everyone).
- **Combat persistence** — the engine serializes the encounter to JSON in a single-row
  `CombatSnapshots` table after each change and reloads on startup, so **an app restart resumes
  the fight**.

## The party (7 PCs)
Winnie Vale (Sorcerer 5), Kennyth (Paladin 5), Boan Strickler (Fighter 5), Gideon Silverspoon
(Bard 5), Job Goodhammer (Paladin 5), Bren Gunning (Druid 5), Korran Vale (Barbarian 4).
(Three earlier test characters — Spurt, Belqorel, Wally — were removed.)

## Run / test / deploy
- **Run locally:** `cd ~/Quail-dnd-simulator/CopilotTest && dotnet run --urls "http://localhost:5179"` → open `/play`.
- **Tests:** `dotnet run --project QuailTests` — headless harness, **293 checks** (character
  builds, level-ups, subclass swaps, combat scenarios, the rules engine, and persistence).
- **Deploy:** on the Mac — `dotnet publish CopilotTest/CopilotTest.csproj -c Release -o /tmp/quail-deploy`
  → `rsync -az --exclude 'dnd-party.db*' /tmp/quail-deploy/ root@198.199.79.46:/opt/quail/`
  → ssh `chown -R quail:quail /opt/quail && systemctl restart quail`. DB migrations run on startup.

## Known limitations / gotchas
- **Blazor Server:** after a *server* restart, open browsers must **refresh** (dead connection);
  on refresh, seat + combat both restore.
- **Don't share the link with the password embedded** (`https://party:pass@…`) — that breaks
  Blazor's startup. Share the plain URL + the password separately.
- Corporate/office wifi may block `*.duckdns.org` (dynamic DNS) — use cellular, or a real domain.
- The rules engine covers the common 80%; the DM adjudicates the long tail (movement, most
  spells, legendary actions are not automated). Only incapacitating conditions auto-block a turn;
  other conditions are tracked labels.
- DuckDNS free domains can lapse if untouched ~30 days (no keep-alive cron set up yet).

## Possible next steps
- Auto-reload clients when the server restarts (avoid the manual refresh).
- DuckDNS keep-alive cron, or move to a real custom domain (less likely to be filter-blocked).
- More rules automation (auto-crit vs prone/paralyzed, condition effects on saves, more standing
  abilities), and curated guide data for any new PCs.

## Recent work (2026-06-28), all live
Guide page folded into the sheet · `AsSplitQuery` perf fix · background/feat ability-reversal
fix · hosting (WAL, Caddy, DigitalOcean) · live multiplayer combat (`/play`) · 4-phase rules
engine · standing-advantage abilities · test-character cleanup · guide-style turn panel · seat
persistence + reclaimable seats + "New Encounter" button · combat persistence to the DB.
