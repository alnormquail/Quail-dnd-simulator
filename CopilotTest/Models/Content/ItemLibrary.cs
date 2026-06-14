namespace CopilotTest.Models.Content;

public enum ItemKind { Weapon, Armor, Shield, Gear, MagicItem }
public enum ArmorWeight { Light, Medium, Heavy }

/// <summary>A piece of equipment from the content library (2024 PHB + a few magic items).</summary>
public record ItemData
{
    public string Key { get; init; } = "";
    public string Name { get; init; } = "";
    public string Source { get; init; } = "PHB 2024";
    public RulesEdition Edition { get; init; } = RulesEdition.Edition2024;
    public ItemKind Kind { get; init; } = ItemKind.Gear;
    public double Weight { get; init; }
    public string Description { get; init; } = "";

    // Weapons
    public string DamageDice { get; init; } = "";
    public DamageType DamageType { get; init; } = DamageType.None;
    public bool Finesse { get; init; }
    public bool Ranged { get; init; }
    public bool TwoHanded { get; init; }
    public string RangeText { get; init; } = "5 ft";
    public string Properties { get; init; } = "";

    // Armor
    public ArmorWeight? ArmorWeight { get; init; }
    public int BaseAC { get; init; }
    public int StrengthRequirement { get; init; }
    public bool StealthDisadvantage { get; init; }

    // Shield
    public int AcBonus { get; init; }

    // Magic
    public bool RequiresAttunement { get; init; }

    public ItemCategory ToCategory() => Kind switch
    {
        ItemKind.Weapon => ItemCategory.Weapon,
        ItemKind.Armor or ItemKind.Shield => ItemCategory.Armor,
        ItemKind.MagicItem => ItemCategory.Treasure,
        _ => ItemCategory.Other,
    };
}

public static class ItemLibrary
{
    public static IReadOnlyList<ItemData> All => _all;
    public static ItemData? Get(string? key) => key is null ? null : _all.FirstOrDefault(i => i.Key == key);
    public static IReadOnlyList<ItemData> ByKind(ItemKind kind) => _all.Where(i => i.Kind == kind).ToList();

    private static readonly List<ItemData> _all =
    [
        // ── Simple weapons ──
        W("club", "Club", "1d4", DamageType.Bludgeoning, "Light", weight: 2),
        W("dagger", "Dagger", "1d4", DamageType.Piercing, "Finesse, Light, Thrown (20/60)", finesse: true, weight: 1, range: "20/60 ft"),
        W("handaxe", "Handaxe", "1d6", DamageType.Slashing, "Light, Thrown (20/60)", weight: 2, range: "20/60 ft"),
        W("javelin", "Javelin", "1d6", DamageType.Piercing, "Thrown (30/120)", weight: 2, range: "30/120 ft"),
        W("mace", "Mace", "1d6", DamageType.Bludgeoning, "—", weight: 4),
        W("quarterstaff", "Quarterstaff", "1d6", DamageType.Bludgeoning, "Versatile (1d8)", weight: 4),
        W("spear", "Spear", "1d6", DamageType.Piercing, "Thrown (20/60), Versatile (1d8)", weight: 3, range: "20/60 ft"),
        W("light-crossbow", "Crossbow, Light", "1d8", DamageType.Piercing, "Ammunition, Loading, Two-Handed, Range (80/320)", ranged: true, twoHanded: true, weight: 5, range: "80/320 ft"),
        W("shortbow", "Shortbow", "1d6", DamageType.Piercing, "Ammunition, Two-Handed, Range (80/320)", ranged: true, twoHanded: true, weight: 2, range: "80/320 ft"),

        // ── Martial weapons ──
        W("battleaxe", "Battleaxe", "1d8", DamageType.Slashing, "Versatile (1d10)", weight: 4),
        W("longsword", "Longsword", "1d8", DamageType.Slashing, "Versatile (1d10)", weight: 3),
        W("greatsword", "Greatsword", "2d6", DamageType.Slashing, "Heavy, Two-Handed", twoHanded: true, weight: 6),
        W("greataxe", "Greataxe", "1d12", DamageType.Slashing, "Heavy, Two-Handed", twoHanded: true, weight: 7),
        W("rapier", "Rapier", "1d8", DamageType.Piercing, "Finesse", finesse: true, weight: 2),
        W("shortsword", "Shortsword", "1d6", DamageType.Piercing, "Finesse, Light", finesse: true, weight: 2),
        W("scimitar", "Scimitar", "1d6", DamageType.Slashing, "Finesse, Light", finesse: true, weight: 3),
        W("warhammer", "Warhammer", "1d8", DamageType.Bludgeoning, "Versatile (1d10)", weight: 2),
        W("maul", "Maul", "2d6", DamageType.Bludgeoning, "Heavy, Two-Handed", twoHanded: true, weight: 10),
        W("glaive", "Glaive", "1d10", DamageType.Slashing, "Heavy, Reach, Two-Handed", twoHanded: true, weight: 6, range: "10 ft"),
        W("longbow", "Longbow", "1d8", DamageType.Piercing, "Ammunition, Heavy, Two-Handed, Range (150/600)", ranged: true, twoHanded: true, weight: 2, range: "150/600 ft"),

        // ── Armor ──
        A("padded", "Padded Armor", Content.ArmorWeight.Light, 11, weight: 8, stealth: true),
        A("leather", "Leather Armor", Content.ArmorWeight.Light, 11, weight: 10),
        A("studded-leather", "Studded Leather", Content.ArmorWeight.Light, 12, weight: 13),
        A("hide", "Hide Armor", Content.ArmorWeight.Medium, 12, weight: 12),
        A("chain-shirt", "Chain Shirt", Content.ArmorWeight.Medium, 13, weight: 20),
        A("scale-mail", "Scale Mail", Content.ArmorWeight.Medium, 14, weight: 45, stealth: true),
        A("breastplate", "Breastplate", Content.ArmorWeight.Medium, 14, weight: 20),
        A("half-plate", "Half Plate", Content.ArmorWeight.Medium, 15, weight: 40, stealth: true),
        A("ring-mail", "Ring Mail", Content.ArmorWeight.Heavy, 14, weight: 40, stealth: true),
        A("chain-mail", "Chain Mail", Content.ArmorWeight.Heavy, 16, weight: 55, strReq: 13, stealth: true),
        A("splint", "Splint Armor", Content.ArmorWeight.Heavy, 17, weight: 60, strReq: 15, stealth: true),
        A("plate", "Plate Armor", Content.ArmorWeight.Heavy, 18, weight: 65, strReq: 15, stealth: true),

        // ── Shield ──
        new() { Key = "shield", Name = "Shield", Kind = ItemKind.Shield, AcBonus = 2, Weight = 6,
            Description = "+2 AC while wielded." },

        // ── A few iconic magic items ──
        new() { Key = "potion-healing", Name = "Potion of Healing", Kind = ItemKind.MagicItem, Weight = 0.5,
            Description = "Regain 2d4 + 2 HP as a Bonus Action to drink." },
        new() { Key = "bag-of-holding", Name = "Bag of Holding", Kind = ItemKind.MagicItem, Weight = 15,
            Description = "Holds up to 500 lb / 64 cubic feet in an extradimensional space." },
        new() { Key = "cloak-of-protection", Name = "Cloak of Protection", Kind = ItemKind.MagicItem, RequiresAttunement = true, Weight = 1,
            Description = "+1 bonus to AC and saving throws while worn (requires attunement)." },
        new() { Key = "ring-of-protection", Name = "Ring of Protection", Kind = ItemKind.MagicItem, RequiresAttunement = true,
            Description = "+1 bonus to AC and saving throws while worn (requires attunement)." },
        new() { Key = "boots-striding-springing", Name = "Boots of Striding and Springing", Kind = ItemKind.MagicItem, RequiresAttunement = true, Weight = 1,
            Description = "Your walking speed becomes 30 ft (if lower) and your jump distance triples." },
        new() { Key = "weapon-plus-1", Name = "+1 Weapon", Kind = ItemKind.MagicItem, Weight = 0,
            Description = "A generic +1 magic weapon: +1 to attack and damage rolls. (Pair with a weapon entry.)" },
        new() { Key = "armor-plus-1", Name = "+1 Armor", Kind = ItemKind.MagicItem, Weight = 0,
            Description = "A generic +1 magic armor: +1 AC over the base armor." },

        // ── Broad DMG magic-item set (common items across rarities) ──────────
        // Potions & oils
        Mi("potion-greater-healing", "Potion of Greater Healing", "Uncommon", "Regain 4d4 + 4 HP.", weight: 0.5),
        Mi("potion-superior-healing", "Potion of Superior Healing", "Rare", "Regain 8d4 + 8 HP.", weight: 0.5),
        Mi("potion-supreme-healing", "Potion of Supreme Healing", "Very Rare", "Regain 10d4 + 20 HP.", weight: 0.5),
        Mi("potion-climbing", "Potion of Climbing", "Common", "Climb speed equal to your speed for 1 hour; advantage on climb checks.", weight: 0.5),
        Mi("potion-fire-breath", "Potion of Fire Breath", "Uncommon", "Bonus Action: exhale fire for 4d6 (DEX save), up to 3 times within 1 hour.", weight: 0.5),
        Mi("potion-giant-strength-hill", "Potion of Hill Giant Strength", "Uncommon", "Your Strength becomes 21 for 1 hour.", weight: 0.5),
        Mi("potion-giant-strength-fire", "Potion of Fire Giant Strength", "Rare", "Your Strength becomes 25 for 1 hour.", weight: 0.5),
        Mi("potion-speed", "Potion of Speed", "Very Rare", "Gain the effects of the Haste spell for 1 minute (no concentration).", weight: 0.5),
        Mi("potion-invisibility", "Potion of Invisibility", "Very Rare", "Become invisible for 1 hour (ends if you attack/cast).", weight: 0.5),
        Mi("potion-flying", "Potion of Flying", "Very Rare", "Gain a flying speed equal to your walking speed for 1 hour.", weight: 0.5),
        Mi("potion-water-breathing", "Potion of Water Breathing", "Uncommon", "Breathe underwater for 1 hour.", weight: 0.5),
        Mi("potion-heroism", "Potion of Heroism", "Rare", "10 temp HP and the Bless effect for 1 hour.", weight: 0.5),
        Mi("oil-sharpness", "Oil of Sharpness", "Very Rare", "Coat a weapon/ammo: it becomes +3 for 1 hour.", weight: 0.5),
        Mi("antitoxin", "Antitoxin", "Common", "Advantage on saves against poison for 1 hour.", weight: 0),
        Mi("spell-scroll", "Spell Scroll", "Varies", "Cast the inscribed spell once without expending a slot (DC/attack scale by level).", weight: 0),

        // Rings
        Mi("ring-free-action", "Ring of Free Action", "Rare", "Difficult terrain costs no extra movement; can't be paralyzed or restrained by magic.", attune: true),
        Mi("ring-spell-storing", "Ring of Spell Storing", "Rare", "Stores up to 5 levels of spells to cast later.", attune: true),
        Mi("ring-regeneration", "Ring of Regeneration", "Very Rare", "Regain 1d6 HP every 10 min; regrow lost body parts.", attune: true),
        Mi("ring-feather-falling", "Ring of Feather Falling", "Rare", "Your fall slows to 60 ft/round; take no falling damage.", attune: true),
        Mi("ring-evasion", "Ring of Evasion", "Rare", "3 charges: use a reaction to succeed on a failed DEX save.", attune: true),
        Mi("ring-the-ram", "Ring of the Ram", "Rare", "3 charges: ranged spectral ram, 2d10 force and push 15 ft.", attune: true),

        // Wands, staffs, rods
        Mi("wand-magic-missiles", "Wand of Magic Missiles", "Uncommon", "7 charges: cast Magic Missile (1 charge = 1st level, +1 per extra).", weight: 1),
        Mi("wand-war-mage-plus1", "Wand of the War Mage, +1", "Uncommon", "+1 to spell attack rolls; ignore half cover.", attune: true, weight: 1),
        Mi("wand-fireballs", "Wand of Fireballs", "Rare", "7 charges: cast Fireball (1 charge = 3rd level, +1 per extra).", attune: true, weight: 1),
        Mi("wand-lightning-bolts", "Wand of Lightning Bolts", "Rare", "7 charges: cast Lightning Bolt (1 charge = 3rd level, +1 per extra).", attune: true, weight: 1),
        Mi("wand-web", "Wand of Web", "Medium-Rare", "7 charges: cast Web (1 charge).", attune: true, weight: 1),
        Mi("staff-healing", "Staff of Healing", "Rare", "10 charges: Cure Wounds, Lesser Restoration, Mass Cure Wounds.", attune: true, weight: 4),
        Mi("staff-fire", "Staff of Fire", "Very Rare", "10 charges: Burning Hands, Fireball, Wall of Fire; resistance to fire.", attune: true, weight: 4),
        Mi("staff-power", "Staff of Power", "Very Rare", "+2 weapon & spell attacks/AC/saves; 20 charges of powerful spells.", attune: true, weight: 4),
        Mi("rod-pact-keeper-plus1", "Rod of the Pact Keeper, +1", "Uncommon", "+1 to warlock spell attacks & save DC; regain a slot 1/long rest.", attune: true, weight: 2),
        Mi("immovable-rod", "Immovable Rod", "Uncommon", "Press the button to fix it in place (holds up to 8,000 lb).", weight: 2),
        Mi("rod-lordly-might", "Rod of Lordly Might", "Legendary", "A +3 mace that transforms into blade/spear/climbing pole and more.", attune: true, weight: 4),

        // Weapons
        new() { Key = "weapon-plus-2", Name = "+2 Weapon", Kind = ItemKind.MagicItem, Description = "[Rare] +2 to attack and damage rolls." },
        new() { Key = "weapon-plus-3", Name = "+3 Weapon", Kind = ItemKind.MagicItem, Description = "[Very Rare] +3 to attack and damage rolls." },
        Mi("flame-tongue", "Flame Tongue", "Rare", "A sword that flares into flame for +2d6 fire damage on hits.", attune: true, weight: 3),
        Mi("frost-brand", "Frost Brand", "Very Rare", "Sword: +1d6 cold damage; resistance to fire; sheds light near flames.", attune: true, weight: 3),
        Mi("sword-of-sharpness", "Sword of Sharpness", "Very Rare", "On a max melee damage roll, +4d6 slashing and can sever limbs.", attune: true, weight: 3),
        Mi("vorpal-sword", "Vorpal Sword", "Legendary", "+3 slashing weapon; on a natural 20, decapitate the target.", attune: true, weight: 3),
        Mi("sun-blade", "Sun Blade", "Rare", "A blade of pure sunlight: +2, 1d8 radiant, extra damage vs undead.", attune: true, weight: 3),
        Mi("dagger-of-venom", "Dagger of Venom", "Rare", "+1 dagger; 1/day coat it to force a DC 15 CON save or 2d10 poison.", attune: false, weight: 1),
        Mi("holy-avenger", "Holy Avenger", "Legendary", "+3 sword (paladin): +2d10 radiant vs fiends/undead; aura of protection.", attune: true, weight: 3),

        // Armor & shields
        new() { Key = "armor-plus-2", Name = "+2 Armor", Kind = ItemKind.MagicItem, Description = "[Rare] +2 AC over the base armor." },
        new() { Key = "armor-plus-3", Name = "+3 Armor", Kind = ItemKind.MagicItem, Description = "[Very Rare] +3 AC over the base armor." },
        Mi("shield-plus-1", "+1 Shield", "Uncommon", "+1 AC in addition to the shield's normal +2.", weight: 6),
        Mi("shield-plus-2", "+2 Shield", "Rare", "+2 AC in addition to the shield's normal +2.", weight: 6),
        Mi("adamantine-armor", "Adamantine Armor", "Uncommon", "Critical hits against you become normal hits.", weight: 0),
        Mi("mithral-armor", "Mithral Armor", "Uncommon", "No Strength requirement and no Stealth disadvantage.", weight: 0),
        Mi("armor-resistance", "Armor of Resistance", "Rare", "Resistance to one chosen damage type while worn.", attune: true),
        Mi("dragon-scale-mail", "Dragon Scale Mail", "Very Rare", "+1 armor; resistance to the dragon's damage type; sense dragons.", attune: true, weight: 45),

        // Wondrous items
        Mi("amulet-of-health", "Amulet of Health", "Rare", "Your Constitution becomes 19 while worn.", attune: true, weight: 1),
        Mi("belt-giant-strength-hill", "Belt of Hill Giant Strength", "Rare", "Your Strength becomes 21 while worn.", attune: true, weight: 1),
        Mi("boots-of-speed", "Boots of Speed", "Rare", "Bonus Action to double your speed; opportunity attacks have disadvantage.", attune: true, weight: 1),
        Mi("boots-elvenkind", "Boots of Elvenkind", "Uncommon", "Advantage on Stealth checks to move silently.", weight: 1),
        Mi("cloak-elvenkind", "Cloak of Elvenkind", "Uncommon", "Advantage on Stealth; others have disadvantage to see you.", attune: true, weight: 1),
        Mi("cloak-of-displacement", "Cloak of Displacement", "Rare", "Attacks against you have disadvantage (until you take damage).", attune: true, weight: 1),
        Mi("gauntlets-ogre-power", "Gauntlets of Ogre Power", "Uncommon", "Your Strength becomes 19 while worn.", attune: true, weight: 2),
        Mi("bracers-of-defense", "Bracers of Defense", "Rare", "+2 AC when wearing no armor and using no shield.", attune: true, weight: 1),
        Mi("headband-of-intellect", "Headband of Intellect", "Uncommon", "Your Intelligence becomes 19 while worn.", attune: true, weight: 1),
        Mi("pearl-of-power", "Pearl of Power", "Uncommon", "1/day, regain one expended spell slot of 3rd level or lower.", attune: true),
        Mi("periapt-wound-closure", "Periapt of Wound Closure", "Uncommon", "Stabilize automatically; double HP from Hit Dice.", attune: true, weight: 1),
        Mi("slippers-spider-climbing", "Slippers of Spider Climbing", "Uncommon", "Walk on walls and ceilings, hands free.", attune: true, weight: 1),
        Mi("winged-boots", "Winged Boots", "Uncommon", "Flying speed equal to your walking speed, up to 4 hours per charge.", attune: true, weight: 1),
        Mi("goggles-of-night", "Goggles of Night", "Uncommon", "Darkvision 60 ft (or +60 ft if you already have it).", weight: 1),
        Mi("hat-of-disguise", "Hat of Disguise", "Uncommon", "Cast Disguise Self at will.", attune: true, weight: 1),
        Mi("broom-of-flying", "Broom of Flying", "Uncommon", "A flying broom (50 ft speed) you can summon and ride.", weight: 3),
        Mi("decanter-endless-water", "Decanter of Endless Water", "Uncommon", "Pours fresh or salt water on command (up to a 30 ft geyser).", weight: 2),
        Mi("driftglobe", "Driftglobe", "Uncommon", "A floating globe that casts Light or Daylight on command.", weight: 1),
        Mi("portable-hole", "Portable Hole", "Rare", "Opens a 6-ft-deep, 6-ft-wide extradimensional hole.", weight: 0),
        Mi("rope-of-climbing", "Rope of Climbing", "Uncommon", "60 ft of rope that moves and anchors on command.", weight: 3),
        Mi("sending-stones", "Sending Stones", "Uncommon", "A matched pair: cast Sending to each other 1/day.", weight: 1),
        Mi("stone-of-good-luck", "Stone of Good Luck (Luckstone)", "Uncommon", "+1 to ability checks and saving throws.", attune: true),
        Mi("necklace-of-fireballs", "Necklace of Fireballs", "Rare", "Detach a bead and throw it as a Fireball (scales with bead).", weight: 0),
        Mi("robe-of-the-archmagi", "Robe of the Archmagi", "Legendary", "Base AC 15 + DEX; advantage on saves vs magic; +2 spell DC.", attune: true, weight: 4),
        Mi("manual-bodily-health", "Manual of Bodily Health", "Very Rare", "Read over 48 hours to raise your Constitution max by 2.", weight: 5),
        Mi("tome-clear-thought", "Tome of Clear Thought", "Very Rare", "Read over 48 hours to raise your Intelligence max by 2.", weight: 5),
        Mi("ioun-stone-protection", "Ioun Stone (Protection)", "Rare", "A stone that orbits your head granting +1 AC.", attune: true),
        Mi("bag-of-tricks", "Bag of Tricks", "Uncommon", "Pull out a fuzzy ball that becomes a random beast ally.", weight: 0.5),
        Mi("eyes-of-the-eagle", "Eyes of the Eagle", "Uncommon", "Advantage on Perception checks that rely on sight.", attune: true),
        Mi("gem-of-seeing", "Gem of Seeing", "Rare", "3 charges: gain Truesight 120 ft for 10 minutes.", attune: true),

        // ── Common gear ──
        new() { Key = "explorers-pack", Name = "Explorer's Pack", Kind = ItemKind.Gear, Weight = 59,
            Description = "Backpack, bedroll, mess kit, tinderbox, 10 torches, 10 days rations, waterskin, 50 ft rope." },
        new() { Key = "healers-kit", Name = "Healer's Kit", Kind = ItemKind.Gear, Weight = 3,
            Description = "10 uses. Stabilize a dying creature without a check." },
        new() { Key = "thieves-tools", Name = "Thieves' Tools", Kind = ItemKind.Gear, Weight = 1,
            Description = "Pick locks and disarm traps (with proficiency)." },
        new() { Key = "rope-hempen", Name = "Rope, Hempen (50 ft)", Kind = ItemKind.Gear, Weight = 10,
            Description = "50 feet of hempen rope." },
        new() { Key = "torch", Name = "Torch", Kind = ItemKind.Gear, Weight = 1,
            Description = "Bright light 20 ft, dim light 20 ft more, for 1 hour." },
    ];

    private static ItemData W(string key, string name, string dice, DamageType dt, string props,
        bool finesse = false, bool ranged = false, bool twoHanded = false, double weight = 0, string range = "5 ft") =>
        new() { Key = key, Name = name, Kind = ItemKind.Weapon, DamageDice = dice, DamageType = dt,
                Properties = props, Finesse = finesse, Ranged = ranged, TwoHanded = twoHanded,
                Weight = weight, RangeText = range };

    private static ItemData A(string key, string name, ArmorWeight armorWeight, int baseAc,
        double weight = 0, int strReq = 0, bool stealth = false) =>
        new() { Key = key, Name = name, Kind = ItemKind.Armor, ArmorWeight = armorWeight, BaseAC = baseAc,
                Weight = weight, StrengthRequirement = strReq, StealthDisadvantage = stealth };

    private static ItemData Mi(string key, string name, string rarity, string desc, bool attune = false, double weight = 0) =>
        new() { Key = key, Name = name, Kind = ItemKind.MagicItem, RequiresAttunement = attune, Weight = weight,
                Description = $"[{rarity}{(attune ? ", attunement" : "")}] {desc}" };
}
