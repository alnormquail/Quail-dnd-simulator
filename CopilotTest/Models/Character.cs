namespace CopilotTest.Models;

public class Character
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public CombatantType Type { get; set; } = CombatantType.PC;

    // Identity
    public string CharacterClass { get; set; } = string.Empty;
    public string Subclass { get; set; } = string.Empty;
    public int CharacterLevel { get; set; } = 1;
    public string Race { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;

    /// <summary>Content-library key of the chosen species (e.g. "high-elf"), if picked from the library.</summary>
    public string SpeciesKey { get; set; } = string.Empty;
    /// <summary>Content-library key of the chosen background, if picked from the library.</summary>
    public string BackgroundKey { get; set; } = string.Empty;
    /// <summary>Content-library key of the chosen subclass, if picked from the library.</summary>
    public string SubclassKey { get; set; } = string.Empty;

    // Ability scores
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Charisma { get; set; } = 10;

    // Combat stats
    public int MaxHitPoints { get; set; } = 10;
    public int ArmorClass { get; set; } = 10;
    public int Speed { get; set; } = 30;
    public int ProficiencyBonus { get; set; } = 2;

    // Saving throw proficiencies (stored as booleans to avoid extra table)
    public bool SaveProfStrength { get; set; } = false;
    public bool SaveProfDexterity { get; set; } = false;
    public bool SaveProfConstitution { get; set; } = false;
    public bool SaveProfIntelligence { get; set; } = false;
    public bool SaveProfWisdom { get; set; } = false;
    public bool SaveProfCharisma { get; set; } = false;

    // Barbarian rage
    public bool IsBarbarianClass { get; set; } = false;
    public int RageBonus { get; set; } = 0;
    public int RageUsesPerDay { get; set; } = 0;

    // Personality & backstory
    public string PersonalityTraits { get; set; } = string.Empty;
    public string Ideals { get; set; } = string.Empty;
    public string Bonds { get; set; } = string.Empty;
    public string Flaws { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // Currency
    public int CopperPieces { get; set; } = 0;
    public int SilverPieces { get; set; } = 0;
    public int ElectrumPieces { get; set; } = 0;
    public int GoldPieces { get; set; } = 0;
    public int PlatinumPieces { get; set; } = 0;

    // Navigation properties (EF Core)
    public List<CombatAction> Actions { get; set; } = new();
    public List<Spell> Spells { get; set; } = new();
    public List<InventoryItem> Inventory { get; set; } = new();
    public List<CharacterSkill> Skills { get; set; } = new();
    public List<SpellSlot> SpellSlots { get; set; } = new();
    public List<CharacterFeature> Features { get; set; } = new();
    public List<AbilityGrant> AbilityGrants { get; set; } = new();

    // Computed display helpers
    public string ClassDisplay => CharacterLevel > 0 && !string.IsNullOrEmpty(CharacterClass)
        ? $"{CharacterClass} {CharacterLevel}"
        : CharacterClass;

    public string HpDisplay => $"{MaxHitPoints} HP";

    public static int GetModifier(int score) => (int)Math.Floor((score - 10) / 2.0);

    public int StrengthModifier     => GetModifier(Strength);
    public int DexterityModifier    => GetModifier(Dexterity);
    public int ConstitutionModifier => GetModifier(Constitution);
    public int IntelligenceModifier => GetModifier(Intelligence);
    public int WisdomModifier       => GetModifier(Wisdom);
    public int CharismaModifier     => GetModifier(Charisma);

    public int PassivePerception => 10 + GetSkillBonus(Skill.Perception);

    public int GetAbilityModifier(AbilityScore ability) => ability switch
    {
        AbilityScore.Strength     => StrengthModifier,
        AbilityScore.Dexterity    => DexterityModifier,
        AbilityScore.Constitution => ConstitutionModifier,
        AbilityScore.Intelligence => IntelligenceModifier,
        AbilityScore.Wisdom       => WisdomModifier,
        AbilityScore.Charisma     => CharismaModifier,
        _                         => 0
    };

    public bool GetSaveProficiency(AbilityScore ability) => ability switch
    {
        AbilityScore.Strength     => SaveProfStrength,
        AbilityScore.Dexterity    => SaveProfDexterity,
        AbilityScore.Constitution => SaveProfConstitution,
        AbilityScore.Intelligence => SaveProfIntelligence,
        AbilityScore.Wisdom       => SaveProfWisdom,
        AbilityScore.Charisma     => SaveProfCharisma,
        _                         => false
    };

    public void SetSaveProficiency(AbilityScore ability, bool value)
    {
        switch (ability)
        {
            case AbilityScore.Strength:     SaveProfStrength     = value; break;
            case AbilityScore.Dexterity:    SaveProfDexterity    = value; break;
            case AbilityScore.Constitution: SaveProfConstitution = value; break;
            case AbilityScore.Intelligence: SaveProfIntelligence = value; break;
            case AbilityScore.Wisdom:       SaveProfWisdom       = value; break;
            case AbilityScore.Charisma:     SaveProfCharisma     = value; break;
        }
    }

    public int GetSaveBonus(AbilityScore ability)
    {
        var mod = GetAbilityModifier(ability);
        return GetSaveProficiency(ability) ? mod + ProficiencyBonus : mod;
    }

    public ProficiencyLevel GetSkillProficiency(Skill skill)
        => Skills.FirstOrDefault(s => s.Skill == skill)?.Proficiency ?? ProficiencyLevel.None;

    public int GetSkillBonus(Skill skill)
    {
        var abilityMod = GetAbilityModifier(SkillAbilityMap.Map[skill]);
        return GetSkillProficiency(skill) switch
        {
            ProficiencyLevel.Proficient => abilityMod + ProficiencyBonus,
            ProficiencyLevel.Expertise  => abilityMod + ProficiencyBonus * 2,
            _                           => abilityMod
        };
    }

    /// <summary>Creates a fresh runtime Combatant from this character template.</summary>
    public Combatant ToCombatant()
    {
        var combatant = new Combatant
        {
            Id               = Id,
            Name             = Name,
            Type             = Type,
            CharacterClass   = CharacterClass,
            CharacterLevel   = CharacterLevel,
            Strength         = Strength,
            Dexterity        = Dexterity,
            Constitution     = Constitution,
            Intelligence     = Intelligence,
            Wisdom           = Wisdom,
            Charisma         = Charisma,
            MaxHitPoints     = MaxHitPoints,
            CurrentHitPoints = MaxHitPoints,
            TemporaryHitPoints = 0,
            ArmorClass       = ArmorClass,
            Speed            = Speed,
            ProficiencyBonus = ProficiencyBonus,
            Initiative       = 0,
            InitiativeRoll   = 0,
            IsDead           = false,
            DeathSaveSuccesses = 0,
            DeathSaveFailures  = 0,
            Conditions       = new HashSet<Condition>(),
            IsBarbarianClass  = IsBarbarianClass,
            RageBonus         = RageBonus,
            RageUsesPerDay    = RageUsesPerDay,
            RageUsesRemaining = RageUsesPerDay,
            IsRaging          = false,
            SpellSlots = SpellSlots
                .Select(s => new SpellSlotState { Level = s.Level, Max = s.MaxSlots, Used = s.UsedSlots })
                .OrderBy(s => s.Level).ToList(),
            Actions = Actions.Select(a => new CombatAction
            {
                Id           = a.Id,
                Name         = a.Name,
                ActionType   = a.ActionType,
                AttackBonus  = a.AttackBonus,
                DamageDice   = a.DamageDice,
                DamageBonus  = a.DamageBonus,
                DamageType   = a.DamageType,
                SaveDC       = a.SaveDC,
                SaveAbility  = a.SaveAbility,
                SpellLevel   = a.SpellLevel,
                Range        = a.Range,
                Description  = a.Description,
                UsesPerDay   = a.UsesPerDay,
                UsesRemaining = a.UsesPerDay
            }).ToList()
        };
        combatant.Pools = CombatResources.BuildPools(combatant);
        return combatant;
    }

    /// <summary>Creates a Character from a runtime Combatant (for saving newly created PCs to the roster).</summary>
    public static Character FromCombatant(Combatant c) => new()
    {
        Id               = c.Id,
        Name             = c.Name,
        Type             = c.Type,
        CharacterClass   = c.CharacterClass,
        CharacterLevel   = c.CharacterLevel,
        Strength         = c.Strength,
        Dexterity        = c.Dexterity,
        Constitution     = c.Constitution,
        Intelligence     = c.Intelligence,
        Wisdom           = c.Wisdom,
        Charisma         = c.Charisma,
        MaxHitPoints     = c.MaxHitPoints,
        ArmorClass       = c.ArmorClass,
        Speed            = c.Speed,
        ProficiencyBonus = c.ProficiencyBonus,
        IsBarbarianClass  = c.IsBarbarianClass,
        RageBonus         = c.RageBonus,
        RageUsesPerDay    = c.RageUsesPerDay,
        Actions = c.Actions.Select(a => new CombatAction
        {
            Name         = a.Name,
            ActionType   = a.ActionType,
            AttackBonus  = a.AttackBonus,
            DamageDice   = a.DamageDice,
            DamageBonus  = a.DamageBonus,
            DamageType   = a.DamageType,
            SaveDC       = a.SaveDC,
            SaveAbility  = a.SaveAbility,
            SpellLevel   = a.SpellLevel,
            Range        = a.Range,
            Description  = a.Description,
            UsesPerDay   = a.UsesPerDay,
            UsesRemaining = a.UsesPerDay
        }).ToList()
    };
}
