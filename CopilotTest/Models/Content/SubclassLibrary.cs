namespace CopilotTest.Models.Content;

/// <summary>
/// 2024 PHB subclasses for all twelve classes. Every subclass carries its
/// defining level-3 feature; the classes the party actually plays (Barbarian,
/// Bard, Cleric, Druid, Fighter, Paladin, Rogue, Sorcerer, Wizard) get deeper
/// feature coverage. Remaining feature text can be filled in incrementally.
/// </summary>
public static class SubclassLibrary
{
    public static IReadOnlyList<SubclassData> All => _all;

    public static IReadOnlyList<SubclassData> ForClass(string className) =>
        _all.Where(s => s.ClassName == className.Trim().ToLowerInvariant()).ToList();

    public static SubclassData? Get(string? key) =>
        key is null ? null : _all.FirstOrDefault(s => s.Key == key);

    private static SubclassData Sc(string key, string name, string cls, params SubclassFeature[] feats) =>
        new() { Key = key, Name = name, ClassName = cls, Features = feats };

    private static SubclassFeature F(int lvl, string name, string desc) => new(lvl, name, desc);

    private static readonly List<SubclassData> _all =
    [
        // ── Barbarian ──
        Sc("barb-berserker", "Path of the Berserker", "barbarian",
            F(3, "Frenzy", "While raging, deal extra damage (dice = your Rage damage bonus) once per turn when you hit with a Strength weapon attack."),
            F(6, "Mindless Rage", "You can't be Charmed or Frightened while raging; effects are suppressed."),
            F(10, "Retaliation", "When a creature within 5 ft damages you, use your Reaction to make a melee attack against it."),
            F(14, "Intimidating Presence", "As a Bonus Action, frighten creatures in a 30-ft cone (WIS save vs your DC).")),
        Sc("barb-wildheart", "Path of the Wild Heart", "barbarian",
            F(3, "Animal Speaker / Rage of the Wilds", "Cast Beast Sense and Speak with Animals as Rituals; choose a Bear, Eagle, or Wolf aspect when you Rage."),
            F(6, "Aspect of the Wilds", "Gain a persistent benefit (Owl darkvision, Panther climb, or Salmon swim)."),
            F(10, "Nature Speaker", "Cast Commune with Nature as a Ritual."),
            F(14, "Power of the Wilds", "While raging, gain Falcon flight, Lion fear aura, or Ram knockdown.")),
        Sc("barb-worldtree", "Path of the World Tree", "barbarian",
            F(3, "Vitality of the Tree", "When you Rage, gain temp HP; each turn you can grant temp HP to an ally."),
            F(6, "Branches of the Tree", "Reaction to pull a creature within 30 ft and reduce its speed to 0."),
            F(10, "Battering Roots", "Your melee weapons gain Push or Topple reach +10 ft on attacks."),
            F(14, "Travel Along the Tree", "Teleport up to 60 ft when you Rage (and bring allies at higher use).")),
        Sc("barb-zealot", "Path of the Zealot", "barbarian",
            F(3, "Divine Fury / Warrior of the Gods", "First hit each turn while raging deals +1d6+½ level radiant/necrotic; you have a pool to be revived more easily."),
            F(6, "Fanatical Focus", "Once per Rage, reroll a failed saving throw."),
            F(10, "Zealous Presence", "Bonus Action: grant up to 10 allies advantage on attacks and saves for a round."),
            F(14, "Rage of the Gods", "Become an avatar of your god: flight, resistance, and ally revival.")),

        // ── Bard ──
        Sc("bard-dance", "College of Dance", "bard",
            F(3, "Dazzling Footwork", "While unarmored, AC uses CHA, and you gain an unarmed Bardic Inspiration die strike + better mobility."),
            F(6, "Inspiring Movement", "Reaction to move and let an ally move when an enemy nears."),
            F(14, "Leading Evasion", "Take no damage on successful DEX saves for half (you and adjacent allies).")),
        Sc("bard-glamour", "College of Glamour", "bard",
            F(3, "Mantle of Inspiration / Enthralling Performance", "Spend Bardic Inspiration to grant allies temp HP + a free move; charm an audience."),
            F(6, "Mantle of Majesty", "Cast Command as a Bonus Action for free each turn for 1 minute."),
            F(14, "Unbreakable Majesty", "Foes must save to attack you; on fail they waste the attack.")),
        Sc("bard-lore", "College of Lore", "bard",
            F(3, "Bonus Proficiencies", "Gain proficiency in three skills of your choice."),
            F(3, "Cutting Words", "Reaction: expend a Bardic Inspiration die to subtract it from an enemy's attack, check, or damage roll."),
            F(6, "Magical Discoveries", "Learn two spells from the Cleric, Druid, or Wizard spell lists."),
            F(14, "Peerless Skill", "Expend a Bardic Inspiration die to add it to your own failed check or attack.")),
        Sc("bard-valor", "College of Valor", "bard",
            F(3, "Combat Inspiration / Martial Training", "Bardic Inspiration can boost damage or AC; gain martial weapons, medium armor, shields."),
            F(6, "Extra Attack", "Attack twice when you take the Attack action."),
            F(14, "Battle Magic", "After casting a spell, make one weapon attack as a Bonus Action.")),

        // ── Cleric ──
        Sc("cleric-life", "Life Domain", "cleric",
            F(3, "Disciple of Life / Life Domain Spells", "Your healing spells restore extra HP (2 + spell level)."),
            F(6, "Blessed Healer", "Your healing spells also heal you."),
            F(10, "Divine Strike", "Once per turn, a weapon hit deals +1d8 radiant."),
            F(14, "Supreme Healing", "Healing dice are maximized instead of rolled.")),
        Sc("cleric-light", "Light Domain", "cleric",
            F(3, "Warding Flare / Radiance of the Dawn", "Reaction to impose disadvantage on an attacker; Channel Divinity blasts radiant damage."),
            F(6, "Improved Warding Flare", "Warding Flare also grants temp HP and regains on a short rest."),
            F(10, "Potent Spellcasting", "Add your WIS modifier to cleric cantrip damage."),
            F(14, "Corona of Light", "Emit sunlight; foes have disadvantage vs your fire/radiant spells.")),
        Sc("cleric-trickery", "Trickery Domain", "cleric",
            F(3, "Blessing of the Trickster / Invoke Duplicity", "Give a creature advantage on Stealth; create an illusory duplicate to flank and cast through."),
            F(6, "Trickster's Transposition", "Swap places with your duplicate as a Bonus Action."),
            F(10, "Divine Strike (Poison)", "Once per turn, a weapon hit deals +1d8 poison."),
            F(14, "Improved Duplicity", "Your duplicate heals allies near it.")),
        Sc("cleric-war", "War Domain", "cleric",
            F(3, "War Priest / Guided Strike", "Make an extra weapon attack as a Bonus Action; Channel Divinity for +10 to hit."),
            F(6, "War God's Blessing", "Channel Divinity to grant +10 to an ally's attack."),
            F(10, "Divine Strike", "Once per turn, a weapon hit deals +1d8 of your deity's damage."),
            F(14, "Avatar of Battle", "Resistance to nonmagical bludgeoning, piercing, and slashing.")),

        // ── Druid ──
        Sc("druid-land", "Circle of the Land", "druid",
            F(3, "Circle of the Land Spells / Land's Aid", "Bonus prepared spells by terrain; expend Wild Shape for a 10-ft AoE that harms foes and heals an ally (2d6)."),
            F(6, "Natural Recovery", "Recover spell slots on a short rest (1/long rest) and always have a terrain spell prepared."),
            F(10, "Nature's Ward", "Immunity to Poisoned; resistance to your terrain's damage type."),
            F(14, "Nature's Sanctuary", "Create a 15-ft protective zone granting cover and resistance.")),
        Sc("druid-moon", "Circle of the Moon", "druid",
            F(3, "Circle Forms", "Wild Shape into stronger beasts (CR up to your level/3) as a Bonus Action; tougher forms."),
            F(6, "Improved Circle Forms", "Add your WIS modifier to Wild Shape HP and make its attacks magical."),
            F(10, "Moonlight Step", "Bonus Action misty-step teleport, proficiency-bonus times per long rest."),
            F(14, "Lunar Form", "Wild Shape attacks deal +2d10 radiant once per turn.")),
        Sc("druid-sea", "Circle of the Sea", "druid",
            F(3, "Wrath of the Sea", "Wild Shape to emanate a damaging aura (CON save, cold/lightning) that can push foes."),
            F(6, "Aquatic Affinity", "Swim speed and bigger aura."),
            F(10, "Stormborn", "Fly speed and resistance to cold, lightning, thunder while the aura is up."),
            F(14, "Oceanic Gift", "Share the aura with an ally.")),
        Sc("druid-stars", "Circle of the Stars", "druid",
            F(3, "Star Map / Starry Form", "Wild Shape into a constellation: Archer (ranged damage), Chalice (healing), or Dragon (concentration/checks)."),
            F(6, "Cosmic Omen", "Reaction to add or subtract a d6 from rolls near you."),
            F(10, "Twinkling Constellations", "Your starry forms improve and you gain flight."),
            F(14, "Full of Stars", "Resistance to bludgeoning, piercing, slashing while in Starry Form.")),

        // ── Fighter ──
        Sc("fighter-battlemaster", "Battle Master", "fighter",
            F(3, "Combat Superiority", "Gain Superiority Dice (d8) and Maneuvers (e.g. Trip, Disarm, Riposte, Precision) to spend on combat tricks."),
            F(7, "Know Your Enemy", "Study a creature to learn its relative strengths."),
            F(10, "Improved Combat Superiority", "Superiority Dice become d10."),
            F(15, "Relentless", "Regain a Superiority Die when you have none on initiative.")),
        Sc("fighter-champion", "Champion", "fighter",
            F(3, "Improved Critical / Remarkable Athlete", "Score critical hits on a 19-20; add half proficiency to initiative and athletic checks, and Heroic Advantage."),
            F(7, "Additional Fighting Style", "Gain a second Fighting Style."),
            F(10, "Heroic Warrior", "Gain Heroic Inspiration each round in combat when you don't have it."),
            F(15, "Superior Critical", "Critical hits on a 18-20.")),
        Sc("fighter-eldritch", "Eldritch Knight", "fighter",
            F(3, "Spellcasting / War Bond", "Learn Wizard spells (mostly abjuration/evocation) and bond weapons you can summon."),
            F(7, "War Magic", "When you use your action to cast a cantrip, make a weapon attack as a Bonus Action."),
            F(10, "Eldritch Strike", "A weapon hit gives the target disadvantage on its next save vs your spells."),
            F(15, "Arcane Charge", "Teleport 30 ft when you Action Surge.")),
        Sc("fighter-psiwarrior", "Psi Warrior", "fighter",
            F(3, "Psionic Power", "Gain Psionic Energy Dice to fuel Protective Field, Psionic Strike (+force damage), and Telekinetic Movement."),
            F(7, "Telekinetic Adept", "Psi-Powered Leap and Telekinetic Thrust (push/knock prone with Psionic Strike)."),
            F(10, "Guarded Mind", "Resistance to psychic damage; shrug off Charmed/Frightened."),
            F(15, "Bulwark of Force", "Grant yourself and allies half cover with telekinetic force.")),

        // ── Monk ──
        Sc("monk-elements", "Warrior of the Elements", "monk",
            F(3, "Elemental Attunement", "Spend Focus to give your strikes reach and elemental damage, and push/pull targets.")),
        Sc("monk-mercy", "Warrior of Mercy", "monk",
            F(3, "Hand of Harm / Hand of Healing", "Spend Focus to deal extra necrotic damage or to heal with your strikes.")),
        Sc("monk-shadow", "Warrior of Shadow", "monk",
            F(3, "Shadow Arts", "Cast Darkness, gain darkvision, and use Minor Illusion; teleport between shadows.")),
        Sc("monk-openhand", "Warrior of the Open Hand", "monk",
            F(3, "Open Hand Technique", "Flurry of Blows can knock prone, push, or deny reactions (DEX save).")),

        // ── Paladin ──
        Sc("pal-devotion", "Oath of Devotion", "paladin",
            F(3, "Sacred Weapon / Oath of Devotion Spells", "Channel Divinity to add CHA to weapon attacks and emit light; always-prepared oath spells (Protection from Evil and Good, Shield of Faith...)."),
            F(7, "Aura of Devotion", "You and allies in your aura can't be Charmed."),
            F(15, "Smite of Protection", "Your Divine Smite also grants you and nearby allies Half Cover."),
            F(20, "Holy Nimbus", "Become wreathed in light: deal radiant damage to nearby foes and gain advantage on saves vs spells."))
            with { GrantedSpells = ["Protection from Evil and Good", "Shield of Faith", "Aid", "Zone of Truth", "Beacon of Hope", "Dispel Magic"] },
        Sc("pal-glory", "Oath of Glory", "paladin",
            F(3, "Inspiring Smite / Peerless Athlete", "Channel Divinity to share temp HP after a Divine Smite; enhance your athletics and jumps."),
            F(7, "Aura of Alacrity", "You and allies who start near you gain +10 ft speed."),
            F(15, "Glorious Defense", "Reaction to add CHA to an ally's AC and counterattack."),
            F(20, "Living Legend", "Advantage on CHA checks, turn a miss into a hit, and bolster saves."))
            with { GrantedSpells = ["Guiding Bolt", "Heroism", "Magic Weapon", "Haste", "Protection from Energy"] },
        Sc("pal-ancients", "Oath of the Ancients", "paladin",
            F(3, "Nature's Wrath / Oath of the Ancients Spells", "Channel Divinity to restrain foes with spectral vines; nature/fey oath spells."),
            F(7, "Aura of Warding", "You and allies in your aura have resistance to damage from spells."),
            F(15, "Undying Sentinel", "Stay at 1 HP when you'd drop to 0 (1/long rest) and don't age."),
            F(20, "Elder Champion", "Transform: regain HP, cast oath spells faster, and weaken foes' saves."))
            with { GrantedSpells = ["Speak with Animals", "Misty Step", "Moonbeam", "Plant Growth", "Protection from Energy"] },
        Sc("pal-vengeance", "Oath of Vengeance", "paladin",
            F(3, "Vow of Enmity / Oath of Vengeance Spells", "Channel Divinity for advantage vs one foe; vengeance oath spells (Hunter's Mark, Hold Person...)."),
            F(7, "Relentless Avenger", "Opportunity-attack hits let you move without provoking."),
            F(15, "Soul of Vengeance", "Reaction attack against your Vow of Enmity target when it attacks."),
            F(20, "Avenging Angel", "Sprout wings, gain flight, and frighten nearby foes."))
            with { GrantedSpells = ["Bane", "Hunter's Mark", "Hold Person", "Misty Step", "Haste"] },
        Sc("pal-opensea", "Oath of the Open Sea", "paladin",
            F(3, "Marine Layer / Oath of the Open Sea Spells", "Channel Divinity to conjure obscuring fog; nautical oath spells."),
            F(7, "Fury of the Tides", "On a hit, push a creature 10 ft and add bonus damage."),
            F(15, "Stormy Waters", "Creatures take damage and are knocked prone when they enter or leave your reach."),
            F(20, "Mythic Swashbuckler", "Climb/jump/swim freely, attack at advantage, and impose disadvantage in response."))
            with { Source = "Spelljammer (AAG)", GrantedSpells = ["Fog Cloud", "Gust of Wind", "Misty Step", "Water Breathing", "Wind Wall", "Water Walk"] },

        // ── Ranger ──
        Sc("ranger-beastmaster", "Beast Master", "ranger",
            F(3, "Primal Companion", "Summon a Beast of the Land, Sea, or Sky that acts on your turn and scales with you.")),
        Sc("ranger-fey", "Fey Wanderer", "ranger",
            F(3, "Dreadful Strikes / Fey Wanderer Spells", "Your weapon attacks deal extra psychic damage; gain Charm Person and fey magic.")),
        Sc("ranger-gloom", "Gloom Stalker", "ranger",
            F(3, "Dread Ambusher / Umbral Sight", "Extra attack and speed on your first turn; invisible in darkness to darkvision.")),
        Sc("ranger-hunter", "Hunter", "ranger",
            F(3, "Hunter's Prey", "Choose Colossus Slayer (+1d8 vs hurt foes), Giant Killer, or Horde Breaker.")),

        // ── Rogue ──
        Sc("rogue-arcane", "Arcane Trickster", "rogue",
            F(3, "Spellcasting / Mage Hand Legerdemain", "Learn Wizard spells (mostly enchantment/illusion) and a stealthy, invisible Mage Hand."),
            F(9, "Magical Ambush", "Foes have disadvantage vs your spells if you're Hidden from them."),
            F(13, "Versatile Trickster", "Use Mage Hand to gain advantage on attacks against a creature."),
            F(17, "Spell Thief", "Steal a spell cast at you (1/long rest).")),
        Sc("rogue-assassin", "Assassin", "rogue",
            F(3, "Assassinate / Assassin's Tools", "Advantage vs creatures that haven't acted; extra damage on the first hit in combat; free disguise & poisoner kits."),
            F(9, "Infiltration Expertise", "Craft believable false identities."),
            F(13, "Envenom Weapons", "Your poison damage is enhanced."),
            F(17, "Death Strike", "Double damage against a surprised target that fails a CON save.")),
        Sc("rogue-soulknife", "Soulknife", "rogue",
            F(3, "Psionic Power / Psychic Blades", "Manifest thrown psychic blades and gain Psionic Energy Dice for boosts and telepathy."),
            F(9, "Soul Blades", "Use dice for Homing Strikes (turn a miss into a hit) and Psychic Teleportation."),
            F(13, "Psychic Veil", "Turn invisible as a Magic action (1/long rest free)."),
            F(17, "Rend Mind", "Force a creature you Sneak Attack to be Stunned (WIS save).")),
        Sc("rogue-thief", "Thief", "rogue",
            F(3, "Fast Hands / Second-Story Work", "Use Cunning Action for Sleight of Hand, Use an Object, or open locks; climb at full speed and jump farther."),
            F(9, "Supreme Sneak", "Cunning Action grants advantage on Stealth and silence."),
            F(13, "Use Magic Device", "Attune to up to 4 items, ignore class/level/species requirements, and boost scrolls."),
            F(17, "Thief's Reflexes", "Take two turns in the first round of combat.")),

        // ── Sorcerer ──
        Sc("sorc-aberrant", "Aberrant Sorcery", "sorcerer",
            F(3, "Telepathic Speech / Aberrant Sorcery Spells", "Telepathy with a creature; always-prepared psionic spells (Arms of Hadar, Detect Thoughts...)."),
            F(6, "Psionic Sorcery", "Cast your subclass spells with Sorcery Points instead of slots, subtly."),
            F(14, "Revelation in Flesh", "Spend Sorcery Points for flight, swim, see invisibility, or squeeze through gaps.")),
        Sc("sorc-clockwork", "Clockwork Sorcery", "sorcerer",
            F(3, "Restore Balance / Clockwork Spells", "Reaction to cancel advantage or disadvantage on a roll; order-themed spells (Aid, Protection from Evil and Good...)."),
            F(6, "Bastion of Law", "Spend Sorcery Points to create a ward absorbing damage (d8 dice)."),
            F(14, "Trance of Order", "Attacks against you can't crit; treat your d20 rolls of 9 or lower as 10.")),
        Sc("sorc-draconic", "Draconic Sorcery", "sorcerer",
            F(3, "Draconic Resilience / Draconic Spells", "+1 HP per level and base AC 13 + DEX; dragon-themed always-prepared spells."),
            F(6, "Elemental Affinity", "Add CHA to one damage roll of your ancestry's type; spend a point for resistance."),
            F(14, "Dragon Wings", "Sprout wings for a flying speed.")),
        Sc("sorc-wildmagic", "Wild Magic Sorcery", "sorcerer",
            F(3, "Wild Magic Surge / Tides of Chaos", "Trigger chaotic d100 surges when you cast; once per long rest gain Advantage on a roll (recharges by surging)."),
            F(6, "Bend Luck", "Reaction: spend 2 Sorcery Points to add or subtract 1d4 from any creature's d20 roll."),
            F(14, "Controlled Chaos", "Roll twice on the Wild Magic Surge table and choose."),
            F(18, "Spell Bombardment", "When you roll max on a damage die, roll it again and add it.")),

        // ── Warlock ──
        Sc("warlock-archfey", "Archfey Patron", "warlock",
            F(3, "Steps of the Fey", "Misty Step a set number of times per long rest, with a bonus effect (refreshing or taunting).")),
        Sc("warlock-celestial", "Celestial Patron", "warlock",
            F(3, "Healing Light / Bonus Cantrips", "A pool of d6 healing dice as a Bonus Action; gain Light, Sacred Flame, and celestial spells.")),
        Sc("warlock-fiend", "Fiend Patron", "warlock",
            F(3, "Dark One's Blessing / Fiend Spells", "Gain temp HP when you drop a foe; fiendish always-prepared spells (Burning Hands, Command...).")),
        Sc("warlock-greatoldone", "Great Old One Patron", "warlock",
            F(3, "Awakened Mind / Psychic Spells", "Telepathy with creatures; gain Dissonant Whispers, Tasha's Hideous Laughter, and other mind spells.")),

        // ── Wizard ──
        Sc("wiz-abjurer", "Abjurer", "wizard",
            F(3, "Arcane Ward / Abjuration Savant", "A rechargeable damage-absorbing ward (2× level + INT); cheaper, faster abjuration scribing."),
            F(6, "Projected Ward", "Use your Arcane Ward to protect allies within 30 ft."),
            F(10, "Spell Breaker", "Always have Counterspell and Dispel Magic prepared and cast them better."),
            F(14, "Spell Resistance", "Advantage on saves vs spells and resistance to spell damage.")),
        Sc("wiz-diviner", "Diviner", "wizard",
            F(3, "Portent / Divination Savant", "Roll two d20s after a long rest and replace any roll with them; cheaper divination scribing."),
            F(6, "Expert Divination", "Casting a divination spell regains a lower-level slot."),
            F(10, "The Third Eye", "Gain darkvision, see invisibility, or read any language."),
            F(14, "Greater Portent", "Roll three Portent dice.")),
        Sc("wiz-evoker", "Evoker", "wizard",
            F(3, "Sculpt Spells / Evocation Savant", "Protect allies from your area evocation spells; cheaper evocation scribing."),
            F(6, "Potent Cantrip", "Targets take half damage from your cantrips even on a successful save."),
            F(10, "Empowered Evocation", "Add your INT modifier to one damage roll of your evocation spells."),
            F(14, "Overchannel", "Maximize a spell's damage (with escalating cost).")),
        Sc("wiz-illusionist", "Illusionist", "wizard",
            F(3, "Improved Illusions / Illusion Savant", "Cast Minor Illusion with both sound and image and at greater range; cheaper illusion scribing."),
            F(6, "Phantasmal Creatures", "Conjure illusory beasts from your spells."),
            F(10, "Illusory Self", "Reaction: an illusion causes an attack against you to miss."),
            F(14, "Illusory Reality", "Make one object of an illusion briefly real.")),
    ];
}
