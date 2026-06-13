namespace CopilotTest.Models.Content;

/// <summary>
/// Read-only library of official D&D content the builder picks from.
/// Leading with the 2024 PHB; every entry is tagged by Source + Edition so
/// 2014 and supplement content can be layered in later, source-filterable.
///
/// NOTE (2024 rules): species grant NO ability score bonuses — those come from
/// backgrounds. Species provide size, speed, darkvision, traits, and the odd
/// proficiency or cantrip.
/// </summary>
public static class ContentLibrary
{
    public static IReadOnlyList<SpeciesData> Species => _species;
    public static SpeciesData? GetSpecies(string? key) =>
        key is null ? null : _species.FirstOrDefault(s => s.Key == key);

    public static IReadOnlyList<BackgroundData> Backgrounds => _backgrounds;
    public static BackgroundData? GetBackground(string? key) =>
        key is null ? null : _backgrounds.FirstOrDefault(b => b.Key == key);

    public static IReadOnlyList<SubclassData> SubclassesForClass(string className) =>
        SubclassLibrary.ForClass(className);
    public static SubclassData? GetSubclass(string? key) => SubclassLibrary.Get(key);

    public static IReadOnlyList<FeatData> Feats => _feats;
    public static FeatData? GetFeat(string? key) =>
        key is null ? null : _feats.FirstOrDefault(f => f.Key == key);
    public static IReadOnlyList<FeatData> FeatsByCategory(FeatCategory cat) =>
        _feats.Where(f => f.Category == cat).ToList();

    private static readonly List<SpeciesData> _species =
    [
        new()
        {
            Key = "human", Name = "Human", DarkvisionFt = 0, Speed = 30,
            Languages = ["Common", "one of your choice"],
            Traits =
            [
                new("Resourceful", "You gain Heroic Inspiration whenever you finish a Long Rest."),
                new("Skillful", "You gain proficiency in one skill of your choice."),
                new("Versatile", "You gain an Origin feat of your choice (Skilled is a common pick)."),
            ],
        },
        new()
        {
            Key = "elf-high", Name = "Elf (High Elf)", DarkvisionFt = 60, Speed = 30,
            Languages = ["Common", "Elvish"],
            SkillProficiencies = [Skill.Perception],
            Traits =
            [
                new("Darkvision 60 ft.", "You can see in dim light within 60 ft as if it were bright light, and in darkness as if dim light (gray only)."),
                new("Fey Ancestry", "You have Advantage on saving throws to avoid or end the Charmed condition."),
                new("Keen Senses", "You have proficiency in the Insight, Perception, or Survival skill (Perception chosen)."),
                new("Trance", "You don't need to sleep; a 4-hour Trance gives the benefits of a Long Rest."),
                new("Elven Lineage: High Elf", "You know the Prestidigitation cantrip. At levels 3 and 5 you gain Detect Magic and Misty Step (1/long rest free, or with slots). Spellcasting ability: INT, WIS, or CHA (choose)."),
            ],
        },
        new()
        {
            Key = "elf-wood", Name = "Elf (Wood Elf)", DarkvisionFt = 60, Speed = 35,
            Languages = ["Common", "Elvish"],
            SkillProficiencies = [Skill.Perception],
            Traits =
            [
                new("Darkvision 60 ft.", "See in dim light within 60 ft as bright, and darkness as dim light."),
                new("Fey Ancestry", "Advantage on saving throws to avoid or end the Charmed condition."),
                new("Keen Senses", "Proficiency in Insight, Perception, or Survival (Perception chosen)."),
                new("Trance", "A 4-hour Trance gives the benefits of a Long Rest."),
                new("Elven Lineage: Wood Elf", "Speed 35 ft. You know the Druidcraft cantrip; at levels 3 and 5 you gain Longstrider and Pass without Trace (1/long rest free, or with slots)."),
            ],
        },
        new()
        {
            Key = "elf-drow", Name = "Elf (Drow)", DarkvisionFt = 120, Speed = 30,
            Languages = ["Common", "Elvish"],
            SkillProficiencies = [Skill.Perception],
            Traits =
            [
                new("Superior Darkvision 120 ft.", "Your darkvision has a range of 120 ft."),
                new("Fey Ancestry", "Advantage on saving throws to avoid or end the Charmed condition."),
                new("Keen Senses", "Proficiency in Insight, Perception, or Survival (Perception chosen)."),
                new("Trance", "A 4-hour Trance gives the benefits of a Long Rest."),
                new("Elven Lineage: Drow", "You know the Dancing Lights cantrip; at levels 3 and 5 you gain Faerie Fire and Darkness (1/long rest free, or with slots)."),
            ],
        },
        new()
        {
            Key = "dwarf", Name = "Dwarf", DarkvisionFt = 120, Speed = 30,
            Languages = ["Common", "Dwarvish"],
            Traits =
            [
                new("Darkvision 120 ft.", "See in dim light within 120 ft as bright, and darkness as dim light."),
                new("Dwarven Resilience", "Resistance to Poison damage; Advantage on saves against being Poisoned."),
                new("Dwarven Toughness", "Your HP maximum increases by 1, and by 1 again each level."),
                new("Stonecunning", "As a Bonus Action, gain Tremorsense 60 ft for 10 minutes while on a stone surface. Uses = proficiency bonus per long rest."),
            ],
        },
        new()
        {
            Key = "gnome-forest", Name = "Gnome (Forest Gnome)", Size = "Small", DarkvisionFt = 60, Speed = 30,
            Languages = ["Common", "Gnomish"],
            Traits =
            [
                new("Darkvision 60 ft.", "See in dim light within 60 ft as bright, and darkness as dim light."),
                new("Gnomish Cunning", "Advantage on INT, WIS, and CHA saving throws."),
                new("Gnomish Lineage: Forest", "You know the Minor Illusion cantrip and always have Speak with Animals prepared (castable 1/long rest free, or with slots)."),
            ],
        },
        new()
        {
            Key = "gnome-rock", Name = "Gnome (Rock Gnome)", Size = "Small", DarkvisionFt = 60, Speed = 30,
            Languages = ["Common", "Gnomish"],
            Traits =
            [
                new("Darkvision 60 ft.", "See in dim light within 60 ft as bright, and darkness as dim light."),
                new("Gnomish Cunning", "Advantage on INT, WIS, and CHA saving throws."),
                new("Gnomish Lineage: Rock", "You know the Mending and Prestidigitation cantrips, and can build Tiny clockwork devices."),
            ],
        },
        new()
        {
            Key = "halfling", Name = "Halfling", Size = "Small", DarkvisionFt = 0, Speed = 30,
            Languages = ["Common", "Halfling"],
            Traits =
            [
                new("Brave", "Advantage on saving throws against being Frightened."),
                new("Halfling Nimbleness", "You can move through the space of any creature larger than you."),
                new("Luck", "When you roll a 1 on the d20 of a D20 Test, you can reroll and must use the new roll."),
                new("Naturally Stealthy", "You can take the Hide action even when obscured only by a creature larger than you."),
            ],
        },
        new()
        {
            Key = "orc", Name = "Orc", DarkvisionFt = 120, Speed = 30,
            Languages = ["Common", "Orc"],
            Traits =
            [
                new("Darkvision 120 ft.", "See in dim light within 120 ft as bright, and darkness as dim light."),
                new("Adrenaline Rush", "Take the Dash action as a Bonus Action and gain temp HP equal to your proficiency bonus. Uses = proficiency bonus per short/long rest."),
                new("Relentless Endurance", "When reduced to 0 HP but not killed, you can drop to 1 HP instead (1/long rest)."),
                new("Powerful Build", "Count as one size larger for carrying capacity and dragging/lifting."),
            ],
        },
        new()
        {
            Key = "dragonborn", Name = "Dragonborn", DarkvisionFt = 60, Speed = 30,
            Languages = ["Common", "Draconic"],
            Traits =
            [
                new("Draconic Ancestry", "Choose a dragon type; it sets your Breath Weapon and resistance damage type."),
                new("Breath Weapon", "Replace one attack with a breath weapon: 1d10 (DEX or CON save) in a 15-ft cone or 30-ft line. Scales with level. Uses = proficiency bonus per long rest."),
                new("Damage Resistance", "Resistance to the damage type of your Draconic Ancestry."),
                new("Darkvision 60 ft.", "See in dim light within 60 ft as bright, and darkness as dim light."),
                new("Draconic Flight (level 5)", "As a Bonus Action, sprout wings for a flying speed equal to your speed for 10 minutes (1/long rest)."),
            ],
        },
        new()
        {
            Key = "tiefling", Name = "Tiefling", DarkvisionFt = 60, Speed = 30,
            Languages = ["Common", "one of your choice"],
            Traits =
            [
                new("Darkvision 60 ft.", "See in dim light within 60 ft as bright, and darkness as dim light."),
                new("Fiendish Legacy", "Choose Abyssal, Chthonic, or Infernal: grants Resistance and a cantrip plus spells at levels 3 and 5."),
                new("Otherworldly Presence", "You know the Thaumaturgy cantrip (spellcasting ability of your Fiendish Legacy choice)."),
            ],
        },
        new()
        {
            Key = "aasimar", Name = "Aasimar", DarkvisionFt = 60, Speed = 30,
            Languages = ["Common", "one of your choice"],
            Traits =
            [
                new("Darkvision 60 ft.", "See in dim light within 60 ft as bright, and darkness as dim light."),
                new("Celestial Resistance", "Resistance to Necrotic and Radiant damage."),
                new("Healing Hands", "As a Magic action, touch a creature and heal d4s equal to your proficiency bonus (1/long rest)."),
                new("Light Bearer", "You know the Light cantrip (spellcasting ability: CHA)."),
                new("Celestial Revelation (level 3)", "Transform as a Bonus Action (Heavenly Wings, Inner Radiance, or Necrotic Shroud) for extra damage. 1/long rest."),
            ],
        },
        new()
        {
            Key = "goliath", Name = "Goliath", Size = "Medium", DarkvisionFt = 0, Speed = 35,
            Languages = ["Common", "Giant"],
            Traits =
            [
                new("Large Form (level 5)", "As a Bonus Action, become Large for 10 minutes if you have room (1/long rest)."),
                new("Giant Ancestry", "Choose a giant boon (e.g. Stone's Endurance, Fire's Burn, Cloud's Jaunt) usable proficiency-bonus times per long rest."),
                new("Powerful Build", "Count as one size larger for carrying capacity and dragging/lifting."),
                new("Speed 35 ft.", "Your walking speed is 35 feet."),
            ],
        },
    ];

    // ── Backgrounds (2024 PHB) ───────────────────────────────────────────────
    private static readonly List<BackgroundData> _backgrounds =
    [
        new() { Key = "acolyte", Name = "Acolyte",
            AbilityOptions = [AbilityScore.Intelligence, AbilityScore.Wisdom, AbilityScore.Charisma],
            SkillProficiencies = [Skill.Insight, Skill.Religion], ToolProficiency = "Calligrapher's Supplies",
            OriginFeatKey = "magic-initiate-cleric", Equipment = "Calligrapher's Supplies, Holy Symbol, Prayer Book, vestments, 8 gp",
            Description = "You devoted yourself to service in a temple, learning sacred rites and lore." },
        new() { Key = "artisan", Name = "Artisan",
            AbilityOptions = [AbilityScore.Strength, AbilityScore.Dexterity, AbilityScore.Intelligence],
            SkillProficiencies = [Skill.Investigation, Skill.Persuasion], ToolProficiency = "Artisan's Tools",
            OriginFeatKey = "crafter", Equipment = "Artisan's Tools, merchant's scale, supplies, 32 gp",
            Description = "You learned a trade as an apprentice and know your way around a workshop." },
        new() { Key = "charlatan", Name = "Charlatan",
            AbilityOptions = [AbilityScore.Dexterity, AbilityScore.Constitution, AbilityScore.Charisma],
            SkillProficiencies = [Skill.Deception, Skill.SleightOfHand], ToolProficiency = "Forgery Kit",
            OriginFeatKey = "skilled", Equipment = "Forgery Kit, fine clothes, disguise, 15 gp",
            Description = "You made your way by guile, scams, and a silver tongue." },
        new() { Key = "criminal", Name = "Criminal",
            AbilityOptions = [AbilityScore.Dexterity, AbilityScore.Constitution, AbilityScore.Intelligence],
            SkillProficiencies = [Skill.SleightOfHand, Skill.Stealth], ToolProficiency = "Thieves' Tools",
            OriginFeatKey = "alert", Equipment = "Thieves' Tools, crowbar, dark clothes with hood, 16 gp",
            Description = "You lived outside the law, surviving by theft and cunning." },
        new() { Key = "entertainer", Name = "Entertainer",
            AbilityOptions = [AbilityScore.Strength, AbilityScore.Dexterity, AbilityScore.Charisma],
            SkillProficiencies = [Skill.Acrobatics, Skill.Performance], ToolProficiency = "Musical Instrument",
            OriginFeatKey = "musician", Equipment = "Musical Instrument, costume, mirror, 11 gp",
            Description = "You thrilled crowds as a performer, musician, or storyteller." },
        new() { Key = "farmer", Name = "Farmer",
            AbilityOptions = [AbilityScore.Strength, AbilityScore.Constitution, AbilityScore.Wisdom],
            SkillProficiencies = [Skill.AnimalHandling, Skill.Nature], ToolProficiency = "Carpenter's Tools",
            OriginFeatKey = "tough", Equipment = "Carpenter's Tools, sickle, healer's kit, iron pot, 30 gp",
            Description = "You grew up working the land, hardy and self-reliant." },
        new() { Key = "guard", Name = "Guard",
            AbilityOptions = [AbilityScore.Strength, AbilityScore.Intelligence, AbilityScore.Wisdom],
            SkillProficiencies = [Skill.Athletics, Skill.Perception], ToolProficiency = "Gaming Set",
            OriginFeatKey = "alert", Equipment = "Gaming Set, spear, light crossbow & 20 bolts, 12 gp",
            Description = "You kept watch over a settlement, gate, or stronghold." },
        new() { Key = "guide", Name = "Guide",
            AbilityOptions = [AbilityScore.Dexterity, AbilityScore.Constitution, AbilityScore.Wisdom],
            SkillProficiencies = [Skill.Stealth, Skill.Survival], ToolProficiency = "Cartographer's Tools",
            OriginFeatKey = "magic-initiate-druid", Equipment = "Cartographer's Tools, shortbow & 20 arrows, fishing tackle, 3 gp",
            Description = "You guided travelers through the wilds and learned its secrets." },
        new() { Key = "hermit", Name = "Hermit",
            AbilityOptions = [AbilityScore.Constitution, AbilityScore.Wisdom, AbilityScore.Charisma],
            SkillProficiencies = [Skill.Medicine, Skill.Religion], ToolProficiency = "Herbalism Kit",
            OriginFeatKey = "healer", Equipment = "Herbalism Kit, quarterstaff, lamp, oil, 16 gp",
            Description = "You lived in seclusion, seeking insight, faith, or solitude." },
        new() { Key = "merchant", Name = "Merchant",
            AbilityOptions = [AbilityScore.Constitution, AbilityScore.Intelligence, AbilityScore.Charisma],
            SkillProficiencies = [Skill.AnimalHandling, Skill.Persuasion], ToolProficiency = "Navigator's Tools",
            OriginFeatKey = "lucky", Equipment = "Navigator's Tools, mule & cart, supplies, 22 gp",
            Description = "You bought, sold, and bartered your way across the land." },
        new() { Key = "noble", Name = "Noble",
            AbilityOptions = [AbilityScore.Strength, AbilityScore.Intelligence, AbilityScore.Charisma],
            SkillProficiencies = [Skill.History, Skill.Persuasion], ToolProficiency = "Gaming Set",
            OriginFeatKey = "skilled", Equipment = "Gaming Set, fine clothes, signet ring, 29 gp",
            Description = "You were born to privilege and trained in courtly manners." },
        new() { Key = "sage", Name = "Sage",
            AbilityOptions = [AbilityScore.Constitution, AbilityScore.Intelligence, AbilityScore.Wisdom],
            SkillProficiencies = [Skill.Arcana, Skill.History], ToolProficiency = "Calligrapher's Supplies",
            OriginFeatKey = "magic-initiate-wizard", Equipment = "Calligrapher's Supplies, books, parchment, 8 gp",
            Description = "You spent years in study, amassing knowledge and arcane lore." },
        new() { Key = "sailor", Name = "Sailor",
            AbilityOptions = [AbilityScore.Strength, AbilityScore.Dexterity, AbilityScore.Wisdom],
            SkillProficiencies = [Skill.Acrobatics, Skill.Perception], ToolProficiency = "Navigator's Tools",
            OriginFeatKey = "tavern-brawler", Equipment = "Navigator's Tools, rope, dagger, fishing tackle, 20 gp",
            Description = "You crewed a ship, weathering storms and hauling cargo." },
        new() { Key = "scribe", Name = "Scribe",
            AbilityOptions = [AbilityScore.Dexterity, AbilityScore.Intelligence, AbilityScore.Wisdom],
            SkillProficiencies = [Skill.Investigation, Skill.Perception], ToolProficiency = "Calligrapher's Supplies",
            OriginFeatKey = "skilled", Equipment = "Calligrapher's Supplies, fine clothes, lamp, parchment, 23 gp",
            Description = "You copied texts and documents with a meticulous hand." },
        new() { Key = "soldier", Name = "Soldier",
            AbilityOptions = [AbilityScore.Strength, AbilityScore.Dexterity, AbilityScore.Constitution],
            SkillProficiencies = [Skill.Athletics, Skill.Intimidation], ToolProficiency = "Gaming Set",
            OriginFeatKey = "savage-attacker", Equipment = "Gaming Set, spear, shortbow & 20 arrows, 14 gp",
            Description = "You served in an army, drilled in discipline and war." },
        new() { Key = "wayfarer", Name = "Wayfarer",
            AbilityOptions = [AbilityScore.Dexterity, AbilityScore.Wisdom, AbilityScore.Charisma],
            SkillProficiencies = [Skill.Insight, Skill.Stealth], ToolProficiency = "Thieves' Tools",
            OriginFeatKey = "lucky", Equipment = "Thieves' Tools, two daggers, gaming set, 16 gp",
            Description = "You survived on the streets or the road by wit and resolve." },
    ];

    // ── Feats ────────────────────────────────────────────────────────────────
    private static readonly List<FeatData> _feats =
    [
        // Origin feats (2024 PHB)
        new() { Key = "alert", Name = "Alert", Category = FeatCategory.Origin,
            Traits = [new("Initiative Proficiency", "Add your proficiency bonus to Initiative rolls."),
                      new("Initiative Swap", "Swap your initiative with a willing ally's after rolling.")] },
        new() { Key = "crafter", Name = "Crafter", Category = FeatCategory.Origin,
            Traits = [new("Tool Proficiency", "Proficiency with three Artisan's Tools of your choice."),
                      new("Discount", "20% discount when buying nonmagical items."),
                      new("Fast Crafting", "Craft certain items during a Long Rest.")] },
        new() { Key = "healer", Name = "Healer", Category = FeatCategory.Origin,
            Traits = [new("Battle Medic", "Use a Healer's Kit as a Utilize action to let a creature spend a Hit Die and heal (Hit Die + prof. bonus)."),
                      new("Healing Reroll", "When you heal with dice, reroll any 1s.")] },
        new() { Key = "lucky", Name = "Lucky", Category = FeatCategory.Origin,
            Traits = [new("Luck Points", "You have luck points equal to your proficiency bonus per long rest."),
                      new("Advantage/Disadvantage", "Spend a luck point to gain Advantage on a d20 Test, or impose Disadvantage on an attack against you.")] },
        new() { Key = "magic-initiate-cleric", Name = "Magic Initiate (Cleric)", Category = FeatCategory.Origin,
            Traits = [new("Two Cantrips", "Learn two Cleric cantrips."),
                      new("1st-level Spell", "Learn one 1st-level Cleric spell, castable once per long rest free (or with slots). Ability: WIS.")] },
        new() { Key = "magic-initiate-druid", Name = "Magic Initiate (Druid)", Category = FeatCategory.Origin,
            Traits = [new("Two Cantrips", "Learn two Druid cantrips."),
                      new("1st-level Spell", "Learn one 1st-level Druid spell, castable once per long rest free (or with slots). Ability: WIS.")] },
        new() { Key = "magic-initiate-wizard", Name = "Magic Initiate (Wizard)", Category = FeatCategory.Origin,
            Traits = [new("Two Cantrips", "Learn two Wizard cantrips."),
                      new("1st-level Spell", "Learn one 1st-level Wizard spell, castable once per long rest free (or with slots). Ability: INT.")] },
        new() { Key = "musician", Name = "Musician", Category = FeatCategory.Origin,
            Traits = [new("Instrument Proficiency", "Proficiency with three Musical Instruments."),
                      new("Encouraging Song", "After a rest, grant Heroic Inspiration to allies (up to your prof. bonus).")] },
        new() { Key = "savage-attacker", Name = "Savage Attacker", Category = FeatCategory.Origin,
            Traits = [new("Savage Attacks", "Once per turn when you hit with a weapon, roll its damage dice twice and use either total.")] },
        new() { Key = "skilled", Name = "Skilled", Category = FeatCategory.Origin,
            Traits = [new("Three Proficiencies", "Gain proficiency in any combination of three skills or tools of your choice.")] },
        new() { Key = "tavern-brawler", Name = "Tavern Brawler", Category = FeatCategory.Origin,
            Traits = [new("Ability Score (choice)", "Increase STR or CON by 1 (max 20) — set this manually on the Stats tab."),
                      new("Improvised Mastery", "Proficiency with improvised weapons; unarmed strike deals 1d4 + STR."),
                      new("Push", "When you hit with an unarmed strike, push the target 5 ft (prof. bonus times/turn).")] },
        new() { Key = "tough", Name = "Tough", Category = FeatCategory.Origin,
            Traits = [new("Tough", "Your HP maximum increases by 2 per character level — set the total on the Stats tab.")] },

        // A selection of popular General feats (level 4+, require an ASI slot)
        new() { Key = "great-weapon-master", Name = "Great Weapon Master", Category = FeatCategory.General, Prerequisite = "Level 4+",
            AbilityBonuses = [new(AbilityScore.Strength, 1)],
            Traits = [new("Heavy Weapon Mastery", "On a hit/kill/crit with a Heavy weapon, make a melee weapon attack as a Bonus Action."),
                      new("Reckless Power", "Once per turn, add your proficiency bonus to a Heavy weapon's damage.")] },
        new() { Key = "sharpshooter", Name = "Sharpshooter", Category = FeatCategory.General, Prerequisite = "Level 4+",
            AbilityBonuses = [new(AbilityScore.Dexterity, 1)],
            Traits = [new("Ignore Cover", "Your ranged attacks ignore half and three-quarters cover."),
                      new("Long Range", "No disadvantage at long range."),
                      new("Bypassing Shot", "Once per turn, take -? for +10 damage (see PHB; 2024 variant differs).")] },
        new() { Key = "war-caster", Name = "War Caster", Category = FeatCategory.General, Prerequisite = "Spellcasting; Level 4+",
            AbilityBonuses = [new(AbilityScore.Intelligence, 1)],
            Traits = [new("Concentration", "Advantage on saves to maintain Concentration."),
                      new("Somatic Hands", "Cast spells even with weapons/shield in hand."),
                      new("Reactive Spell", "Cast a spell (not just a melee attack) as an opportunity-attack reaction.")] },
        new() { Key = "sentinel", Name = "Sentinel", Category = FeatCategory.General, Prerequisite = "Level 4+",
            AbilityBonuses = [new(AbilityScore.Strength, 1)],
            Traits = [new("Stop Right There", "Opportunity-attack hit reduces target's speed to 0 for the turn."),
                      new("Guardian", "Reaction attack when a creature within 5 ft attacks someone other than you.")] },
        new() { Key = "mobile", Name = "Mobile", Category = FeatCategory.General, Prerequisite = "Level 4+",
            AbilityBonuses = [new(AbilityScore.Dexterity, 1)],
            Traits = [new("Speed +10", "Your speed increases by 10 feet."),
                      new("Nimble Escape", "No opportunity attacks from a creature you made a melee attack against this turn.")] },
        new() { Key = "resilient", Name = "Resilient", Category = FeatCategory.General, Prerequisite = "Level 4+",
            Traits = [new("Ability + Save (choice)", "Increase one ability by 1 (max 20) and gain saving-throw proficiency in it — set the ability bump on the Stats tab and tick the save.")] },
    ];
}
