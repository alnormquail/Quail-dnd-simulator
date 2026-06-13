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
}
