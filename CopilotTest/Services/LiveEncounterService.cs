using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using CopilotTest.Data;
using CopilotTest.Models;

namespace CopilotTest.Services;

/// <summary>
/// The shared LIVE-TABLE encounter: a lightweight initiative tracker for real play.
/// Unlike <see cref="CombatEngineService"/> (the solo combat simulator, which auto-rolls
/// attacks and enforces rules), this service only does bookkeeping — turn order, HP,
/// condition labels, death saves, resources — and leaves all adjudication to the DM.
/// Singleton shared by every connected circuit; mutators are lock-guarded.
/// </summary>
public class LiveEncounterService : IDisposable
{
    private readonly Random _random = new();
    private readonly CharacterService _characterService;
    private readonly IDbContextFactory<DndDbContext> _dbFactory;
    private bool _loaded;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    // Row 1 in CombatSnapshots belongs to the simulator engine; the live table saves here.
    private const int SnapshotRowId = 2;

    private readonly object _gate = new();

    public List<Combatant> Combatants { get; private set; } = new();
    public List<CombatLog> Log { get; private set; } = new();
    public CombatState State { get; private set; } = CombatState.Setup;
    public int CurrentRound { get; private set; } = 0;
    public int CurrentTurnIndex { get; private set; } = 0;

    public Combatant? CurrentCombatant =>
        ActiveCombatants.Count > 0 ? ActiveCombatants[CurrentTurnIndex % ActiveCombatants.Count] : null;

    // Dead PCs and downed monsters drop out of the turn order; unconscious PCs stay in
    // (they roll death saves on their turn).
    public List<Combatant> ActiveCombatants =>
        Combatants.Where(c => !c.IsDead && !(c.Type != CombatantType.PC && c.CurrentHitPoints <= 0)).ToList();

    public List<Character> SavedRoster { get; private set; } = new();

    public event Action? OnStateChanged;

    public LiveEncounterService(CharacterService characterService, IDbContextFactory<DndDbContext> dbFactory)
    {
        _characterService = characterService;
        _dbFactory = dbFactory;
        SavedRoster = _characterService.GetPCs();
        LoadPersistedState();
        _loaded = true;
    }

    public void RefreshRosterFromDb()
    {
        SavedRoster = _characterService.GetPCs();
        NotifyChanged();
    }

    // ── Persistence: the live encounter survives an app restart ─────────────

    private sealed class Snapshot
    {
        public List<Combatant> Combatants { get; set; } = new();
        public List<CombatLog> Log { get; set; } = new();
        public CombatState State { get; set; }
        public int CurrentRound { get; set; }
        public int CurrentTurnIndex { get; set; }
    }

    private void NotifyChanged()
    {
        // Invoke each subscriber separately: one dead circuit's handler throwing
        // must not stop the rest of the table from updating.
        if (OnStateChanged is { } handlers)
        {
            foreach (Action h in handlers.GetInvocationList())
            {
                try { h(); } catch { /* dead circuit — ignore */ }
            }
        }
        SchedulePersist();
    }

    // Persistence is debounced onto a timer-thread: button mashing coalesces into
    // one write, and no circuit's UI thread ever waits on the DB.
    private readonly object _persistGate = new();
    private Timer? _persistTimer;

    private void SchedulePersist()
    {
        if (!_loaded) return;
        lock (_persistGate)
        {
            _persistTimer ??= new Timer(_ => PersistState(), null, Timeout.Infinite, Timeout.Infinite);
            _persistTimer.Change(250, Timeout.Infinite);
        }
    }

    /// <summary>Flush the pending snapshot on graceful shutdown (systemd restart/deploy).</summary>
    public void Dispose()
    {
        lock (_persistGate) { _persistTimer?.Dispose(); _persistTimer = null; }
        PersistState();
    }

    private void PersistState()
    {
        if (!_loaded) return;
        string json;
        lock (_gate)
        {
            var snap = new Snapshot
            {
                Combatants = Combatants,
                Log = Log.Count > 300 ? Log.Skip(Log.Count - 300).ToList() : Log,
                State = State,
                CurrentRound = CurrentRound,
                CurrentTurnIndex = CurrentTurnIndex,
            };
            json = JsonSerializer.Serialize(snap, JsonOpts);
        }
        try
        {
            using var db = _dbFactory.CreateDbContext();
            var row = db.CombatSnapshots.Find(SnapshotRowId);
            if (row == null) db.CombatSnapshots.Add(new CombatSnapshot { Id = SnapshotRowId, Json = json });
            else row.Json = json;
            db.SaveChanges();
        }
        catch { /* best-effort: a DB hiccup must never break the table */ }
    }

    private void LoadPersistedState()
    {
        try
        {
            using var db = _dbFactory.CreateDbContext();
            var row = db.CombatSnapshots.AsNoTracking().FirstOrDefault(s => s.Id == SnapshotRowId);
            if (row == null || string.IsNullOrWhiteSpace(row.Json)) return;
            var snap = JsonSerializer.Deserialize<Snapshot>(row.Json, JsonOpts);
            if (snap == null) return;
            Combatants = snap.Combatants ?? new();
            Log = snap.Log ?? new();
            State = snap.State;
            CurrentRound = snap.CurrentRound;
            CurrentTurnIndex = snap.CurrentTurnIndex;
        }
        catch { /* corrupt/incompatible snapshot — start fresh rather than crash */ }
    }

    // ── Encounter setup ──────────────────────────────────────────────────────

    public Combatant? Find(Guid id) => Combatants.FirstOrDefault(c => c.Id == id);

    public void AddCombatant(Combatant combatant)
    {
        lock (_gate) { Combatants.Add(combatant); }
        NotifyChanged();
    }

    public void RemoveCombatant(Guid id)
    {
        lock (_gate) { Combatants.RemoveAll(c => c.Id == id); }
        NotifyChanged();
    }

    public void NewEncounter()
    {
        SavedRoster = _characterService.GetPCs();
        lock (_gate)
        {
            Combatants.Clear();
            foreach (var template in SavedRoster)
                Combatants.Add(template.ToCombatant());
            Log.Clear();
            State = CombatState.Setup;
            CurrentRound = 0;
            CurrentTurnIndex = 0;
        }
        NotifyChanged();
    }

    /// <summary>Roll initiative for everyone (d20 + DEX) and begin. The DM can
    /// override any result afterwards with <see cref="SetInitiative"/>.</summary>
    public void StartCombat()
    {
        lock (_gate)
        {
            if (Combatants.Count < 2) return;
            State = CombatState.Active;
            CurrentRound = 1;
            CurrentTurnIndex = 0;

            foreach (var c in Combatants)
            {
                var roll = RollD20();
                c.InitiativeRoll = roll;
                c.Initiative = roll + c.DexterityModifier;
                c.ReactionAvailable = true;
            }
            SortByInitiative();

            Log.Clear();
            AddLog(1, "Combat", "⚔️ Combat begins! Round 1", LogEntryType.RoundStart);
            // Hidden combatants are masked in the log — the log is visible to everyone.
            foreach (var c in Combatants.Where(c => !c.IsHiddenFromPlayers))
                AddLog(1, c.Name, $"Initiative: {c.InitiativeRoll} + {c.DexterityModifier} = {c.Initiative}", LogEntryType.Info);
            AddLog(1, "Combat", $"Turn order: {string.Join(" → ", Combatants.Select(c => c.IsHiddenFromPlayers ? "???" : c.Name))}", LogEntryType.Info);
        }
        NotifyChanged();
    }

    /// <summary>DM override: set a combatant's initiative total and re-sort the order.
    /// Keeps the turn pointer on whoever is currently acting.</summary>
    public void SetInitiative(Guid id, int value)
    {
        lock (_gate)
        {
            var c = Combatants.FirstOrDefault(x => x.Id == id);
            if (c == null) return;
            var currentId = CurrentCombatant?.Id;
            c.Initiative = value;
            SortByInitiative();
            if (State == CombatState.Active && currentId.HasValue)
            {
                var idx = ActiveCombatants.FindIndex(x => x.Id == currentId.Value);
                if (idx >= 0) CurrentTurnIndex = idx;
            }
            AddLog(CurrentRound, c.Name, $"initiative set to {value}", LogEntryType.Info);
        }
        NotifyChanged();
    }

    private void SortByInitiative() =>
        Combatants = Combatants
            .OrderByDescending(c => c.Initiative)
            .ThenByDescending(c => c.DexterityModifier)
            .ToList();

    // ── Turn order ───────────────────────────────────────────────────────────

    public bool IsCurrentTurn(Guid charId) =>
        State == CombatState.Active && CurrentCombatant?.Id == charId;

    /// <summary>Who acts after the current combatant (null when combat isn't running).</summary>
    public Combatant? NextCombatant
    {
        get
        {
            if (State != CombatState.Active) return null;
            var active = ActiveCombatants;
            if (active.Count < 2) return null;
            return active[(CurrentTurnIndex + 1) % active.Count];
        }
    }

    /// <summary>How many turns until this combatant acts (0 = acting now, -1 = not in the order).</summary>
    public int TurnsUntil(Guid charId)
    {
        if (State != CombatState.Active) return -1;
        var active = ActiveCombatants;
        if (active.Count == 0) return -1;
        var idx = active.FindIndex(c => c.Id == charId);
        if (idx < 0) return -1;
        var cur = CurrentTurnIndex % active.Count;
        return (idx - cur + active.Count) % active.Count;
    }

    /// <summary>Announce a player's guide dice roll to the whole table's log.</summary>
    public void AnnounceRoll(string actor, string message)
    {
        lock (_gate) { AddLog(CurrentRound, actor, message, LogEntryType.Roll); }
        NotifyChanged();
    }

    /// <summary>Advance to the next turn (DM control).</summary>
    public void NextTurn()
    {
        lock (_gate)
        {
            if (State != CombatState.Active) return;
            AdvanceTurn();
        }
        NotifyChanged();
    }

    /// <summary>A player ends their own turn (only allowed when it IS their turn).</summary>
    public void EndTurn(Guid actingCharId)
    {
        lock (_gate)
        {
            if (!IsCurrentTurn(actingCharId)) return;
            AdvanceTurn();
        }
        NotifyChanged();
    }

    private void AdvanceTurn()
    {
        var active = ActiveCombatants;
        if (active.Count == 0) return;
        CurrentTurnIndex++;
        if (CurrentTurnIndex >= active.Count)
        {
            CurrentRound++;
            CurrentTurnIndex = 0;
            foreach (var c in Combatants) c.ReactionAvailable = true;   // reactions refresh each round
            AddLog(CurrentRound, "Combat", $"━━━ Round {CurrentRound} ━━━", LogEntryType.RoundStart);
        }
    }

    /// <summary>End the encounter (DM). No auto-detection — the table decides when it's over.</summary>
    public void EndCombat()
    {
        lock (_gate)
        {
            if (State == CombatState.Setup) return;
            foreach (var c in Combatants.Where(c => c.IsRaging)) c.IsRaging = false;
            State = CombatState.Finished;
            AddLog(CurrentRound, "Combat", "🏁 The DM ends the encounter.", LogEntryType.RoundStart);
        }
        NotifyChanged();
    }

    // ── Bookkeeping (DM adjudicates; the app just records) ──────────────────

    /// <summary>Manual HP change: positive heals, negative deals damage. Damage is taken
    /// as entered — the DM applies resistance/vulnerability math at the table.</summary>
    public void AdjustHp(Guid id, int delta)
    {
        lock (_gate)
        {
            var c = Combatants.FirstOrDefault(x => x.Id == id);
            if (c == null) return;
            if (delta >= 0)
            {
                if (c.CurrentHitPoints <= 0 && c.Type == CombatantType.PC && delta > 0)
                { c.DeathSaveSuccesses = 0; c.DeathSaveFailures = 0; }   // brought back up
                c.CurrentHitPoints = Math.Min(c.MaxHitPoints, c.CurrentHitPoints + delta);
                AddLog(CurrentRound, c.Name, $"heals {delta} → {c.HpDisplay} HP", LogEntryType.Info);
            }
            else
            {
                var damage = -delta;
                if (c.TemporaryHitPoints > 0)
                {
                    var absorbed = Math.Min(c.TemporaryHitPoints, damage);
                    c.TemporaryHitPoints -= absorbed;
                    damage -= absorbed;
                }
                c.CurrentHitPoints = Math.Max(0, c.CurrentHitPoints - damage);
                AddLog(CurrentRound, c.Name,
                    $"takes {-delta} damage — {c.HpDisplay} HP remaining. {c.StatusDisplay}", LogEntryType.Damage);
                if (c.CurrentHitPoints <= 0)
                {
                    if (c.Type == CombatantType.PC)
                        AddLog(CurrentRound, c.Name, "😵 falls unconscious and must make death saving throws!", LogEntryType.Kill);
                    else
                    {
                        c.IsDead = true;
                        AddLog(CurrentRound, c.Name, "💀 is defeated!", LogEntryType.Kill);
                    }
                }
            }
        }
        NotifyChanged();
    }

    public void SetTempHp(Guid id, int temp)
    {
        lock (_gate)
        {
            var c = Combatants.FirstOrDefault(x => x.Id == id);
            if (c == null) return;
            c.TemporaryHitPoints = Math.Max(0, temp);
            AddLog(CurrentRound, c.Name, $"gains {c.TemporaryHitPoints} temporary HP", LogEntryType.Info);
        }
        NotifyChanged();
    }

    /// <summary>Toggle a condition LABEL. Purely informational — nothing is enforced;
    /// the DM adjudicates what the condition actually does.</summary>
    public void ToggleCondition(Guid id, Condition condition)
    {
        lock (_gate)
        {
            var c = Combatants.FirstOrDefault(x => x.Id == id);
            if (c == null) return;
            if (c.Conditions.Remove(condition))
                AddLog(CurrentRound, c.Name, $"is no longer {condition}", LogEntryType.Condition);
            else
            {
                c.Conditions.Add(condition);
                AddLog(CurrentRound, c.Name, $"is now {condition}", LogEntryType.Condition);
            }
        }
        NotifyChanged();
    }

    /// <summary>Toggle the Raging flag (tracked as a label + uses; no damage math here).</summary>
    public void ToggleRaging(Guid id)
    {
        lock (_gate)
        {
            var c = Combatants.FirstOrDefault(x => x.Id == id);
            if (c == null || !c.IsBarbarianClass) return;
            if (c.IsRaging)
            {
                c.IsRaging = false;
                AddLog(CurrentRound, c.Name, "🔥 Rage ends.", LogEntryType.Info);
            }
            else if (c.RageUsesRemaining > 0)
            {
                c.IsRaging = true;
                c.RageUsesRemaining--;
                AddLog(CurrentRound, c.Name,
                    $"🔥 enters a RAGE! [{c.RageUsesRemaining} rage(s) remaining]", LogEntryType.Info);
            }
        }
        NotifyChanged();
    }

    /// <summary>DM: hide/reveal a combatant from the players' view (surprise monsters).
    /// Revealing announces it in the log; hiding is silent (no spoilers).</summary>
    public void ToggleHidden(Guid id)
    {
        lock (_gate)
        {
            var c = Combatants.FirstOrDefault(x => x.Id == id);
            if (c == null || c.Type == CombatantType.PC) return;
            c.IsHiddenFromPlayers = !c.IsHiddenFromPlayers;
            if (!c.IsHiddenFromPlayers)
                AddLog(CurrentRound, c.Name, "👁 appears!", LogEntryType.Condition);
        }
        NotifyChanged();
    }

    /// <summary>Spend a reaction (tracked + logged; the DM adjudicates the effect).</summary>
    public void UseReaction(Guid id)
    {
        lock (_gate)
        {
            var c = Combatants.FirstOrDefault(x => x.Id == id);
            if (c == null || !c.ReactionAvailable) return;
            c.ReactionAvailable = false;
            AddLog(CurrentRound, c.Name, "uses their reaction.", LogEntryType.Info);
        }
        NotifyChanged();
    }

    public void SetSlotUsed(Guid id, int level, int used)
    {
        lock (_gate)
        {
            var slot = Combatants.FirstOrDefault(x => x.Id == id)?.SpellSlots.FirstOrDefault(s => s.Level == level);
            if (slot == null) return;
            slot.Used = Math.Clamp(used, 0, slot.Max);
        }
        NotifyChanged();
    }

    public void SetPoolCurrent(Guid id, string poolName, int current)
    {
        lock (_gate)
        {
            var pool = Combatants.FirstOrDefault(x => x.Id == id)?.Pools.FirstOrDefault(p => p.Name == poolName);
            if (pool == null) return;
            pool.Current = Math.Clamp(current, 0, pool.Max);
        }
        NotifyChanged();
    }

    // ── Death saves: auto-rolled by default, DM can override the tallies ────

    public void RollDeathSaveFor(Guid id)
    {
        lock (_gate)
        {
            var c = Combatants.FirstOrDefault(x => x.Id == id);
            if (c == null || c.Type != CombatantType.PC || !c.IsUnconscious) return;
            var roll = RollD20();
            if (roll == 20)
            {
                c.CurrentHitPoints = 1;
                c.DeathSaveSuccesses = 0;
                c.DeathSaveFailures = 0;
                AddLog(CurrentRound, c.Name, "🌟 Death save: rolled 20 — regains 1 HP and stabilizes!", LogEntryType.DeathSave);
            }
            else if (roll == 1)
            {
                c.DeathSaveFailures += 2;
                AddLog(CurrentRound, c.Name, $"💀 Death save: rolled 1 — TWO failures! ({c.DeathSaveSuccesses}✓/{c.DeathSaveFailures}✗)", LogEntryType.DeathSave);
            }
            else if (roll >= 10)
            {
                c.DeathSaveSuccesses++;
                AddLog(CurrentRound, c.Name, $"✅ Death save: rolled {roll} — success! ({c.DeathSaveSuccesses}✓/{c.DeathSaveFailures}✗)", LogEntryType.DeathSave);
            }
            else
            {
                c.DeathSaveFailures++;
                AddLog(CurrentRound, c.Name, $"❌ Death save: rolled {roll} — failure! ({c.DeathSaveSuccesses}✓/{c.DeathSaveFailures}✗)", LogEntryType.DeathSave);
            }
            ResolveDeathSaves(c);
        }
        NotifyChanged();
    }

    /// <summary>DM override: record a death save rolled at the table (true = success).</summary>
    public void RecordDeathSave(Guid id, bool success)
    {
        lock (_gate)
        {
            var c = Combatants.FirstOrDefault(x => x.Id == id);
            if (c == null || c.Type != CombatantType.PC || !c.IsUnconscious) return;
            if (success)
            {
                c.DeathSaveSuccesses++;
                AddLog(CurrentRound, c.Name, $"✅ Death save success (DM) ({c.DeathSaveSuccesses}✓/{c.DeathSaveFailures}✗)", LogEntryType.DeathSave);
            }
            else
            {
                c.DeathSaveFailures++;
                AddLog(CurrentRound, c.Name, $"❌ Death save failure (DM) ({c.DeathSaveSuccesses}✓/{c.DeathSaveFailures}✗)", LogEntryType.DeathSave);
            }
            ResolveDeathSaves(c);
        }
        NotifyChanged();
    }

    /// <summary>DM override: clear the tallies (e.g. stabilized by a Medicine check).</summary>
    public void ClearDeathSaves(Guid id)
    {
        lock (_gate)
        {
            var c = Combatants.FirstOrDefault(x => x.Id == id);
            if (c == null) return;
            c.DeathSaveSuccesses = 0;
            c.DeathSaveFailures = 0;
            AddLog(CurrentRound, c.Name, "death saves reset (DM)", LogEntryType.DeathSave);
        }
        NotifyChanged();
    }

    private void ResolveDeathSaves(Combatant c)
    {
        if (c.DeathSaveSuccesses >= 3)
        {
            c.DeathSaveSuccesses = 0;
            c.DeathSaveFailures = 0;
            AddLog(CurrentRound, c.Name, "💪 stabilizes after 3 successes!", LogEntryType.DeathSave);
        }
        else if (c.DeathSaveFailures >= 3)
        {
            c.IsDead = true;
            AddLog(CurrentRound, c.Name, "💀 dies after 3 death save failures!", LogEntryType.Kill);
        }
    }

    // ── Reads / helpers ──────────────────────────────────────────────────────

    public IReadOnlyList<Combatant> SnapshotCombatants() { lock (_gate) { return Combatants.ToList(); } }
    public IReadOnlyList<CombatLog> SnapshotLog() { lock (_gate) { return Log.ToList(); } }

    private int RollD20() => _random.Next(1, 21);

    private void AddLog(int round, string actor, string message, LogEntryType type)
    {
        lock (_gate)
        {
            Log.Add(new CombatLog { Round = round, ActorName = actor, Message = message, EntryType = type });
        }
    }

    public string GetEntryIcon(LogEntryType type) => type switch
    {
        LogEntryType.Hit       => "⚔️",
        LogEntryType.Miss      => "🛡️",
        LogEntryType.Damage    => "🩸",
        LogEntryType.Kill      => "💀",
        LogEntryType.DeathSave => "🎲",
        LogEntryType.Condition => "⚠️",
        LogEntryType.RoundStart=> "📜",
        LogEntryType.Roll      => "🎲",
        _                      => "ℹ️"
    };

    public string GetEntryClass(LogEntryType type) => type switch
    {
        LogEntryType.Hit       => "log-hit",
        LogEntryType.Miss      => "log-miss",
        LogEntryType.Damage    => "log-damage",
        LogEntryType.Kill      => "log-kill",
        LogEntryType.DeathSave => "log-death-save",
        LogEntryType.RoundStart=> "log-round",
        LogEntryType.Roll      => "log-death-save",   // gold dice styling
        _                      => "log-info"
    };
}
