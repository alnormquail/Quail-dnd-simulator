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
}
