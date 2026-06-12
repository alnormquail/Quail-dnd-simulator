using CopilotTest.Models;

namespace CopilotTest.Services;

public class CombatEngineService
{
    private readonly Random _random = new();
    private readonly CharacterService _characterService;
    private List<Combatant> _lastEncounterMonsters = new();

    public List<Combatant> Combatants { get; private set; } = new();
    public List<CombatLog> Log { get; private set; } = new();
    public CombatState State { get; private set; } = CombatState.Setup;
    public int CurrentRound { get; private set; } = 0;
    public int CurrentTurnIndex { get; private set; } = 0;
    public Combatant? CurrentCombatant => ActiveCombatants.Count > 0 ? ActiveCombatants[CurrentTurnIndex % ActiveCombatants.Count] : null;

    public List<Combatant> ActiveCombatants =>
        Combatants.Where(c => !c.IsDead && !(c.Type != CombatantType.PC && c.CurrentHitPoints <= 0)).ToList();

    /// <summary>
    /// Persistent roster of PC/NPC characters, loaded from the database.
    /// Each entry is a Character (template) — ToCombatant() is called to create encounter copies.
    /// </summary>
    public List<Character> SavedRoster { get; private set; } = new();

    public CombatEngineService(CharacterService characterService)
    {
        _characterService = characterService;
        RefreshRosterFromDb();
    }

    public void RefreshRosterFromDb()
    {
        SavedRoster = _characterService.GetPCs();
        OnStateChanged?.Invoke();
    }

    public event Action? OnStateChanged;

    public void AddCombatant(Combatant combatant)
    {
        Combatants.Add(combatant);
        OnStateChanged?.Invoke();
    }

    public void RemoveCombatant(Guid id)
    {
        Combatants.RemoveAll(c => c.Id == id);
        OnStateChanged?.Invoke();
    }

    public void SaveToRoster(Combatant combatant)
    {
        if (combatant.Type == CombatantType.Monster) return;
        if (SavedRoster.Any(r => r.Id == combatant.Id)) return;
        var character = Character.FromCombatant(combatant);
        _characterService.Create(character);
        RefreshRosterFromDb();
    }

    public void RemoveFromRoster(Guid id)
    {
        _characterService.Delete(id);
        Combatants.RemoveAll(c => c.Id == id);
        RefreshRosterFromDb();
    }

    public bool IsInRoster(Guid id) => SavedRoster.Any(r => r.Id == id);

    public void AddFromRoster(Guid rosterId)
    {
        var template = SavedRoster.FirstOrDefault(r => r.Id == rosterId);
        if (template == null) return;
        if (Combatants.Any(c => c.Id == rosterId)) return;
        Combatants.Add(template.ToCombatant());
        OnStateChanged?.Invoke();
    }

    public void NewEncounter()
    {
        RefreshRosterFromDb();
        Combatants.Clear();
        foreach (var template in SavedRoster)
            Combatants.Add(template.ToCombatant());
        Log.Clear();
        State = CombatState.Setup;
        CurrentRound = 0;
        CurrentTurnIndex = 0;
        OnStateChanged?.Invoke();
    }

    public void RetryCombat()
    {
        if (_lastEncounterMonsters.Count == 0) return;

        Combatants.Clear();
        foreach (var template in SavedRoster)
            Combatants.Add(template.ToCombatant());
        foreach (var monsterTemplate in _lastEncounterMonsters)
            Combatants.Add(CloneFresh(monsterTemplate));
        Log.Clear();
        CurrentRound = 0;
        CurrentTurnIndex = 0;
        State = CombatState.Setup;
        OnStateChanged?.Invoke();
        StartCombat();
    }

    public void EnterRage(Combatant combatant)
    {
        if (!combatant.IsBarbarianClass) return;
        if (combatant.IsRaging) return;
        if (combatant.RageUsesRemaining <= 0) return;
        if (State != CombatState.Active) return;

        combatant.IsRaging = true;
        combatant.RageUsesRemaining--;
        AddLog(CurrentRound, combatant.Name,
            $"🔥 enters a RAGE! (+{combatant.RageBonus} melee damage, resistance to physical damage) "
            + $"[{combatant.RageUsesRemaining} rage(s) remaining]",
            LogEntryType.Info);
        OnStateChanged?.Invoke();
    }

    private void EndAllRages()
    {
        foreach (var c in Combatants.Where(c => c.IsRaging))
        {
            c.IsRaging = false;
            AddLog(CurrentRound, c.Name, "🔥 Rage ends.", LogEntryType.Info);
        }
    }

    private static Combatant CloneFresh(Combatant src) => new()
    {
        Id = src.Id,
        Name = src.Name,
        Type = src.Type,
        CharacterClass = src.CharacterClass,
        CharacterLevel = src.CharacterLevel,
        Strength = src.Strength, Dexterity = src.Dexterity, Constitution = src.Constitution,
        Intelligence = src.Intelligence, Wisdom = src.Wisdom, Charisma = src.Charisma,
        MaxHitPoints = src.MaxHitPoints,
        CurrentHitPoints = src.MaxHitPoints,
        TemporaryHitPoints = 0,
        ArmorClass = src.ArmorClass,
        Speed = src.Speed,
        ProficiencyBonus = src.ProficiencyBonus,
        Initiative = 0, InitiativeRoll = 0,
        IsDead = false, DeathSaveSuccesses = 0, DeathSaveFailures = 0,
        Conditions = new HashSet<Condition>(),
        IsBarbarianClass = src.IsBarbarianClass,
        RageBonus = src.RageBonus,
        RageUsesPerDay = src.RageUsesPerDay,
        RageUsesRemaining = src.RageUsesPerDay,
        IsRaging = false,
        Actions = src.Actions.Select(a => new CombatAction
        {
            Id = a.Id,
            Name = a.Name, ActionType = a.ActionType, AttackBonus = a.AttackBonus,
            DamageDice = a.DamageDice, DamageBonus = a.DamageBonus, DamageType = a.DamageType,
            SaveDC = a.SaveDC, SaveAbility = a.SaveAbility, SpellLevel = a.SpellLevel,
            Range = a.Range, Description = a.Description,
            UsesPerDay = a.UsesPerDay, UsesRemaining = a.UsesPerDay
        }).ToList()
    };

    public void StartCombat()
    {
        if (Combatants.Count < 2)
            return;

        State = CombatState.Active;
        CurrentRound = 1;
        CurrentTurnIndex = 0;

        _lastEncounterMonsters = Combatants
            .Where(c => c.Type == CombatantType.Monster)
            .Select(c => CloneFresh(c))
            .ToList();

        foreach (var c in Combatants)
        {
            var roll = RollD20();
            c.InitiativeRoll = roll;
            c.Initiative = roll + c.DexterityModifier;
        }

        Combatants = Combatants
            .OrderByDescending(c => c.Initiative)
            .ThenByDescending(c => c.DexterityModifier)
            .ThenBy(_ => Guid.NewGuid())
            .ToList();

        Log.Clear();
        AddLog(1, "Combat", $"⚔️ Combat begins! Round 1", LogEntryType.RoundStart);

        foreach (var c in Combatants)
            AddLog(1, c.Name, $"Initiative: {c.InitiativeRoll} + {c.DexterityModifier} = {c.Initiative}", LogEntryType.Info);

        AddLog(1, "Combat", $"Turn order: {string.Join(" → ", Combatants.Select(c => c.Name))}", LogEntryType.Info);

        OnStateChanged?.Invoke();
    }

    public void ResetCombat()
    {
        Combatants.Clear();
        Log.Clear();
        State = CombatState.Setup;
        CurrentRound = 0;
        CurrentTurnIndex = 0;
        OnStateChanged?.Invoke();
    }

    public void SimulateRound()
    {
        if (State != CombatState.Active) return;

        var active = ActiveCombatants;
        if (active.Count == 0) return;

        AddLog(CurrentRound, "Combat", $"━━━ Round {CurrentRound} ━━━", LogEntryType.RoundStart);

        foreach (var attacker in active.ToList())
        {
            if (attacker.IsDead || (attacker.Type != CombatantType.PC && attacker.CurrentHitPoints <= 0))
                continue;

            if (attacker.Type == CombatantType.PC && attacker.IsUnconscious)
            {
                PerformDeathSave(attacker);
                continue;
            }

            if (attacker.Conditions.Contains(Condition.Incapacitated) ||
                attacker.Conditions.Contains(Condition.Paralyzed) ||
                attacker.Conditions.Contains(Condition.Stunned) ||
                attacker.Conditions.Contains(Condition.Unconscious))
            {
                AddLog(CurrentRound, attacker.Name, $"is {string.Join(", ", attacker.Conditions)} and cannot act.", LogEntryType.Condition);
                continue;
            }

            var target = PickTarget(attacker);
            if (target == null)
            {
                AddLog(CurrentRound, attacker.Name, "has no valid targets.", LogEntryType.Info);
                continue;
            }

            PerformAttack(attacker, target);
        }

        if (CheckCombatEnd())
        {
            EndAllRages();
            State = CombatState.Finished;
            var winners = ActiveCombatants;
            var winnerNames = winners.Count > 0 ? string.Join(", ", winners.Select(c => c.Name)) : "Nobody";
            AddLog(CurrentRound, "Combat", $"🏆 Combat over! Survivors: {winnerNames}", LogEntryType.RoundStart);
            OnStateChanged?.Invoke();
            return;
        }

        CurrentRound++;
        CurrentTurnIndex = 0;
        OnStateChanged?.Invoke();
    }

    public void SimulateTurn()
    {
        if (State != CombatState.Active) return;

        var active = ActiveCombatants;
        if (active.Count == 0) return;

        var attacker = active[CurrentTurnIndex % active.Count];

        if (attacker.Type == CombatantType.PC && attacker.IsUnconscious)
        {
            PerformDeathSave(attacker);
        }
        else if (!attacker.Conditions.Contains(Condition.Incapacitated) &&
                 !attacker.Conditions.Contains(Condition.Paralyzed) &&
                 !attacker.Conditions.Contains(Condition.Stunned))
        {
            var target = PickTarget(attacker);
            if (target != null)
                PerformAttack(attacker, target);
            else
                AddLog(CurrentRound, attacker.Name, "has no valid targets.", LogEntryType.Info);
        }
        else
        {
            AddLog(CurrentRound, attacker.Name, $"is incapacitated and skips their turn.", LogEntryType.Condition);
        }

        AdvanceTurn(active);
        OnStateChanged?.Invoke();
    }

    public void ExecuteActionForCurrent(CombatAction action)
    {
        if (State != CombatState.Active) return;
        var active = ActiveCombatants;
        if (active.Count == 0) return;

        var attacker = active[CurrentTurnIndex % active.Count];
        var target = PickTarget(attacker);
        if (target == null)
        {
            AddLog(CurrentRound, attacker.Name, "has no valid targets.", LogEntryType.Info);
        }
        else
        {
            if (action.IsLimited)
                action.UsesRemaining = Math.Max(0, action.UsesRemaining - 1);

            switch (action.ActionType)
            {
                case ActionType.Attack:
                case ActionType.SpellAttack:
                    PerformAttackRoll(attacker, target, action);
                    break;
                case ActionType.Spell:
                    PerformSpellSave(attacker, target, action);
                    break;
                default:
                    AddLog(CurrentRound, attacker.Name, $"uses {action.Name}: {action.Description}", LogEntryType.Info);
                    break;
            }
        }

        AdvanceTurn(active);
        OnStateChanged?.Invoke();
    }

    public List<Combatant> GetCurrentTargets()
    {
        var active = ActiveCombatants;
        if (active.Count == 0) return new();
        var attacker = active[CurrentTurnIndex % active.Count];
        if (attacker.Type == CombatantType.Monster)
            return Combatants.Where(c => c.Type != CombatantType.Monster && !c.IsDead && c.CurrentHitPoints > 0).ToList();
        return Combatants.Where(c => c.Type == CombatantType.Monster && !c.IsDead && c.CurrentHitPoints > 0).ToList();
    }

    public void ExecuteActionAgainstTarget(CombatAction action, Combatant target)
    {
        if (State != CombatState.Active) return;
        var active = ActiveCombatants;
        if (active.Count == 0) return;

        var attacker = active[CurrentTurnIndex % active.Count];

        if (action.IsLimited)
            action.UsesRemaining = Math.Max(0, action.UsesRemaining - 1);

        switch (action.ActionType)
        {
            case ActionType.Attack:
            case ActionType.SpellAttack:
                PerformAttackRoll(attacker, target, action);
                break;
            case ActionType.Spell:
                PerformSpellSave(attacker, target, action);
                break;
            default:
                AddLog(CurrentRound, attacker.Name, $"uses {action.Name}: {action.Description}", LogEntryType.Info);
                break;
        }

        AdvanceTurn(active);
        OnStateChanged?.Invoke();
    }

    private void AdvanceTurn(List<Combatant> active)
    {
        CurrentTurnIndex++;
        if (CurrentTurnIndex >= active.Count)
        {
            if (CheckCombatEnd())
            {
                EndAllRages();
                State = CombatState.Finished;
                var winners = ActiveCombatants;
                var winnerNames = winners.Count > 0 ? string.Join(", ", winners.Select(c => c.Name)) : "Nobody";
                AddLog(CurrentRound, "Combat", $"🏆 Combat over! Survivors: {winnerNames}", LogEntryType.RoundStart);
            }
            else
            {
                CurrentRound++;
                CurrentTurnIndex = 0;
                AddLog(CurrentRound, "Combat", $"━━━ Round {CurrentRound} ━━━", LogEntryType.RoundStart);
            }
        }
    }

    private void PerformAttack(Combatant attacker, Combatant target)
    {
        var action = ChooseAction(attacker, target);
        if (action == null)
        {
            AddLog(CurrentRound, attacker.Name, "has no available actions!", LogEntryType.Info);
            return;
        }

        if (action.IsLimited)
            action.UsesRemaining = Math.Max(0, action.UsesRemaining - 1);

        switch (action.ActionType)
        {
            case ActionType.Attack:
            case ActionType.SpellAttack:
                PerformAttackRoll(attacker, target, action);
                break;
            case ActionType.Spell:
                PerformSpellSave(attacker, target, action);
                break;
            default:
                AddLog(CurrentRound, attacker.Name, $"uses {action.Name}: {action.Description}", LogEntryType.Info);
                break;
        }
    }

    private CombatAction? ChooseAction(Combatant attacker, Combatant target)
    {
        var usable = attacker.Actions.Where(a => a.CanUse).ToList();
        if (usable.Count == 0) return null;

        double hpPct       = attacker.MaxHitPoints > 0 ? (double)attacker.CurrentHitPoints / attacker.MaxHitPoints : 1.0;
        double targetHpPct = target.MaxHitPoints > 0 ? (double)target.CurrentHitPoints / target.MaxHitPoints : 1.0;
        bool attIsHurt     = hpPct < 0.4;
        bool targetIsLow   = targetHpPct < 0.25;

        double ExpectedDmg(CombatAction a)
        {
            var m = System.Text.RegularExpressions.Regex.Match(a.DamageDice, @"(\d+)d(\d+)");
            if (!m.Success) return a.DamageBonus;
            double avg = int.Parse(m.Groups[1].Value) * (int.Parse(m.Groups[2].Value) + 1) / 2.0;
            return avg + a.DamageBonus;
        }

        if (targetIsLow)
            return usable.OrderByDescending(ExpectedDmg).First();

        if (attIsHurt)
        {
            var free = usable.Where(a => !a.IsLimited).ToList();
            if (free.Count > 0)
                return free.OrderByDescending(ExpectedDmg).First();
        }

        var limitedSpells = usable.Where(a => a.IsLimited && a.SpellLevel > 0).ToList();
        if (limitedSpells.Count > 0 && _random.NextDouble() < 0.70)
        {
            var maxLevel = limitedSpells.Max(a => a.SpellLevel);
            var topSpells = limitedSpells.Where(a => a.SpellLevel == maxLevel).ToList();
            return topSpells[_random.Next(topSpells.Count)];
        }

        var freeActions = usable.Where(a => !a.IsLimited).OrderByDescending(ExpectedDmg).ToList();
        if (freeActions.Count > 1)
            return freeActions[_random.Next(Math.Min(2, freeActions.Count))];
        if (freeActions.Count == 1) return freeActions[0];

        return usable.OrderByDescending(ExpectedDmg).First();
    }

    private void PerformAttackRoll(Combatant attacker, Combatant target, CombatAction action)
    {
        int rageDmgBonus = (attacker.IsRaging && attacker.RageBonus > 0
                            && action.ActionType == ActionType.Attack) ? attacker.RageBonus : 0;

        var result = ResolveAttackRoll(action, target, rageDmgBonus);
        var critTag = result.CriticalHit ? " 💥 CRITICAL HIT!" : result.CriticalMiss ? " 😬 CRITICAL MISS!" : "";
        var rollDesc = $"[d20: {result.AttackRoll}] Total: {result.TotalAttackRoll} vs AC {target.ArmorClass}";
        var dmgType = action.DamageType != DamageType.None ? $" {action.DamageType}" : "";

        if (result.Hit)
        {
            AddLog(CurrentRound, attacker.Name,
                $"uses {action.DisplayName} on {target.Name} — {rollDesc}{critTag} → Hit for {result.TotalDamage}{dmgType} damage! ({result.DiceRolled})"
                + (rageDmgBonus > 0 ? $" [+{rageDmgBonus} Rage]" : ""),
                LogEntryType.Hit);
            ApplyDamage(target, result.TotalDamage, action.DamageType);
        }
        else
        {
            AddLog(CurrentRound, attacker.Name,
                $"uses {action.DisplayName} on {target.Name} — {rollDesc}{critTag} → Miss!",
                LogEntryType.Miss);
        }
    }

    private void PerformSpellSave(Combatant attacker, Combatant target, CombatAction action)
    {
        var saveBonus = GetSaveBonus(target, action.SaveAbility);
        var saveRoll = RollD20();
        var totalSave = saveRoll + saveBonus;
        var saved = totalSave >= action.SaveDC;
        var dmgType = action.DamageType != DamageType.None ? $" {action.DamageType}" : "";
        var spellDesc = action.SpellLevel > 0 ? $"Level {action.SpellLevel} spell" : "cantrip";

        var (rawDmg, diceDesc) = RollDice(action.DamageDice, false);
        var fullDamage = Math.Max(1, rawDmg + action.DamageBonus);
        var actualDamage = saved ? fullDamage / 2 : fullDamage;

        AddLog(CurrentRound, attacker.Name,
            $"casts {action.DisplayName} ({spellDesc}) — DC {action.SaveDC} {action.SaveAbility} save: {target.Name} rolled {saveRoll}+{saveBonus}={totalSave} → {(saved ? "Saved! Half damage" : "Failed save!")}",
            LogEntryType.Hit);
        AddLog(CurrentRound, target.Name,
            $"takes {actualDamage}{dmgType} damage from {action.Name} ({diceDesc}{(saved ? ", halved" : "")})",
            LogEntryType.Damage);

        ApplyDamage(target, actualDamage, action.DamageType);
    }

    private static int GetSaveBonus(Combatant c, AbilityScore ability) => ability switch
    {
        AbilityScore.Strength     => Combatant.GetModifier(c.Strength),
        AbilityScore.Dexterity    => Combatant.GetModifier(c.Dexterity),
        AbilityScore.Constitution => Combatant.GetModifier(c.Constitution),
        AbilityScore.Intelligence => Combatant.GetModifier(c.Intelligence),
        AbilityScore.Wisdom       => Combatant.GetModifier(c.Wisdom),
        AbilityScore.Charisma     => Combatant.GetModifier(c.Charisma),
        _ => 0
    };

    public AttackResult ResolveAttackRoll(CombatAction action, Combatant target, int extraDamageBonus = 0)
    {
        var attackRoll = RollD20();
        var totalAttack = attackRoll + action.AttackBonus;
        var isCritHit = attackRoll == 20;
        var isCritMiss = attackRoll == 1;
        var hit = isCritHit || (!isCritMiss && totalAttack >= target.ArmorClass);

        var result = new AttackResult
        {
            AttackRoll = attackRoll,
            TotalAttackRoll = totalAttack,
            CriticalHit = isCritHit,
            CriticalMiss = isCritMiss,
            Hit = hit
        };

        if (hit)
        {
            var (diceResult, diceDesc) = RollDice(action.DamageDice, isCritHit);
            result.DamageRoll = diceResult;
            result.DiceRolled = diceDesc;
            result.TotalDamage = Math.Max(1, diceResult + action.DamageBonus + extraDamageBonus);
        }

        return result;
    }

    public AttackResult ResolveAttack(Combatant attacker, Combatant target)
    {
        if (attacker.PrimaryAction != null)
            return ResolveAttackRoll(attacker.PrimaryAction, target);
        return new AttackResult { Hit = false };
    }

    private void ApplyDamage(Combatant target, int damage, DamageType damageType = DamageType.None)
    {
        if (target.IsRaging && damageType is DamageType.Slashing or DamageType.Piercing or DamageType.Bludgeoning)
        {
            damage = Math.Max(1, damage / 2);
            AddLog(CurrentRound, target.Name, $"🔥 Rage resistance halves physical damage → {damage}", LogEntryType.Info);
        }

        if (target.TemporaryHitPoints > 0)
        {
            var absorbed = Math.Min(target.TemporaryHitPoints, damage);
            target.TemporaryHitPoints -= absorbed;
            damage -= absorbed;
        }

        target.CurrentHitPoints = Math.Max(0, target.CurrentHitPoints - damage);

        AddLog(CurrentRound, target.Name,
            $"takes {damage} damage — {target.HpDisplay} HP remaining. {target.StatusDisplay}",
            LogEntryType.Damage);

        if (target.CurrentHitPoints <= 0)
        {
            if (target.Type == CombatantType.PC)
                AddLog(CurrentRound, target.Name, $"😵 falls unconscious and must make death saving throws!", LogEntryType.Kill);
            else
            {
                target.IsDead = true;
                AddLog(CurrentRound, target.Name, $"💀 is defeated!", LogEntryType.Kill);
            }
        }
    }

    private void PerformDeathSave(Combatant pc)
    {
        var roll = RollD20();
        if (roll == 20)
        {
            pc.CurrentHitPoints = 1;
            pc.DeathSaveSuccesses = 0;
            pc.DeathSaveFailures = 0;
            AddLog(CurrentRound, pc.Name, $"🌟 Death save: rolled 20 — regains 1 HP and stabilizes!", LogEntryType.DeathSave);
        }
        else if (roll == 1)
        {
            pc.DeathSaveFailures += 2;
            AddLog(CurrentRound, pc.Name, $"💀 Death save: rolled 1 — TWO failures! ({pc.DeathSaveSuccesses}✓/{pc.DeathSaveFailures}✗)", LogEntryType.DeathSave);
        }
        else if (roll >= 10)
        {
            pc.DeathSaveSuccesses++;
            AddLog(CurrentRound, pc.Name, $"✅ Death save: rolled {roll} — success! ({pc.DeathSaveSuccesses}✓/{pc.DeathSaveFailures}✗)", LogEntryType.DeathSave);
        }
        else
        {
            pc.DeathSaveFailures++;
            AddLog(CurrentRound, pc.Name, $"❌ Death save: rolled {roll} — failure! ({pc.DeathSaveSuccesses}✓/{pc.DeathSaveFailures}✗)", LogEntryType.DeathSave);
        }

        if (pc.DeathSaveSuccesses >= 3)
        {
            pc.DeathSaveSuccesses = 0;
            pc.DeathSaveFailures = 0;
            AddLog(CurrentRound, pc.Name, $"💪 stabilizes after 3 successes!", LogEntryType.DeathSave);
        }
        else if (pc.DeathSaveFailures >= 3)
        {
            pc.IsDead = true;
            AddLog(CurrentRound, pc.Name, $"💀 dies after 3 death save failures!", LogEntryType.Kill);
        }
    }

    private Combatant? PickTarget(Combatant attacker)
    {
        List<Combatant> candidates;

        if (attacker.Type == CombatantType.Monster)
            candidates = Combatants.Where(c => c.Type != CombatantType.Monster && !c.IsDead && c.CurrentHitPoints > 0).ToList();
        else if (attacker.Type == CombatantType.NPC)
            candidates = Combatants.Where(c => c.Type == CombatantType.Monster && !c.IsDead && c.CurrentHitPoints > 0).ToList();
        else
            candidates = Combatants.Where(c => c.Type == CombatantType.Monster && !c.IsDead && c.CurrentHitPoints > 0).ToList();

        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        var nearDead = candidates.Where(c => c.MaxHitPoints > 0 && (double)c.CurrentHitPoints / c.MaxHitPoints < 0.25).ToList();
        if (nearDead.Count > 0)
            return nearDead.OrderBy(c => c.CurrentHitPoints).First();

        if (_random.NextDouble() < 0.50)
        {
            return candidates
                .OrderByDescending(c => c.Actions.Count > 0 ? c.Actions.Max(a => a.AttackBonus) : 0)
                .ThenBy(c => c.CurrentHitPoints)
                .First();
        }

        return candidates.OrderBy(c => c.CurrentHitPoints).First();
    }

    private bool CheckCombatEnd()
    {
        var monstersAlive = Combatants.Any(c => c.Type == CombatantType.Monster && !c.IsDead && c.CurrentHitPoints > 0);
        var heroesAlive = Combatants.Any(c => c.Type != CombatantType.Monster && !c.IsDead && c.CurrentHitPoints > 0);
        return !monstersAlive || !heroesAlive;
    }

    private int RollD20() => _random.Next(1, 21);

    private (int total, string desc) RollDice(string diceExpr, bool doubled = false)
    {
        try
        {
            diceExpr = diceExpr.Trim().ToLower();
            int total = 0;
            var rolls = new List<int>();

            var parts = diceExpr.Split('+');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Contains('d'))
                {
                    var diceParts = trimmed.Split('d');
                    int count = int.TryParse(diceParts[0], out var c) ? c : 1;
                    int sides = int.TryParse(diceParts[1], out var s) ? s : 6;
                    if (doubled) count *= 2;
                    for (int i = 0; i < count; i++)
                    {
                        var r = _random.Next(1, sides + 1);
                        rolls.Add(r);
                        total += r;
                    }
                }
                else if (int.TryParse(trimmed, out var flat))
                {
                    total += flat;
                }
            }

            var desc = string.Join("+", rolls.Select(r => r.ToString()));
            if (doubled) desc = $"[CRIT: doubled dice] {desc}";
            return (total, desc);
        }
        catch
        {
            var fallback = _random.Next(1, 7);
            return (fallback, $"1d6={fallback}");
        }
    }

    private void AddLog(int round, string actor, string message, LogEntryType type)
    {
        Log.Add(new CombatLog
        {
            Round = round,
            ActorName = actor,
            Message = message,
            EntryType = type
        });
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
        _                      => "log-info"
    };
}
