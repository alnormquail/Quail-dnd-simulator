namespace CopilotTest.Models.Content;

/// <summary>
/// One line of a starting-equipment kit. If <see cref="LibraryKey"/> is set it
/// resolves to an ItemLibrary entry (so weapons/armor wire up attacks/AC);
/// otherwise <see cref="Name"/> is added as a plain inventory entry (focuses,
/// packs, spellbooks, etc. that don't need stats).
/// </summary>
public record KitItem(string? LibraryKey, string Name, int Quantity = 1);

/// <summary>
/// 2024 PHB class starting-equipment kits — the "recommended starting
/// equipment." Class-appropriate by design (a Wizard's kit has no battleaxe).
/// Background equipment is handled separately from BackgroundData.Equipment.
/// </summary>
public static class StartingKits
{
    public static IReadOnlyList<KitItem> ForClass(string characterClass)
    {
        var key = characterClass.Trim().ToLowerInvariant();
        return _kits.TryGetValue(key, out var kit) ? kit : [];
    }

    public static bool HasKit(string characterClass) =>
        _kits.ContainsKey(characterClass.Trim().ToLowerInvariant());

    private static KitItem L(string libraryKey, int qty = 1) =>
        new(libraryKey, ItemLibrary.Get(libraryKey)?.Name ?? libraryKey, qty);
    private static KitItem P(string name, int qty = 1) => new(null, name, qty);

    private static readonly Dictionary<string, List<KitItem>> _kits = new()
    {
        ["barbarian"] = [L("greataxe"), L("handaxe", 4), P("Explorer's Pack")],
        ["bard"]      = [L("leather"), L("dagger", 2), P("Musical Instrument"), P("Entertainer's Pack")],
        ["cleric"]    = [L("chain-shirt"), L("shield"), L("mace"), P("Holy Symbol"), P("Priest's Pack")],
        ["druid"]     = [L("leather"), L("quarterstaff"), P("Druidic Focus"), P("Herbalism Kit"), P("Explorer's Pack")],
        ["fighter"]   = [L("chain-mail"), L("greatsword"), L("javelin", 8), P("Dungeoneer's Pack")],
        ["monk"]      = [L("spear"), L("dagger", 5), P("Explorer's Pack")],
        ["paladin"]   = [L("chain-mail"), L("shield"), L("longsword"), L("javelin", 6), P("Holy Symbol"), P("Priest's Pack")],
        ["ranger"]    = [L("studded-leather"), L("scimitar"), L("shortsword"), L("longbow"), P("Arrows (20)"), P("Druidic Focus"), P("Explorer's Pack")],
        ["rogue"]     = [L("leather"), L("dagger", 2), L("shortsword"), L("shortbow"), P("Arrows (20)"), L("thieves-tools"), P("Burglar's Pack")],
        ["sorcerer"]  = [L("spear"), L("dagger", 2), P("Arcane Focus"), P("Dungeoneer's Pack")],
        ["warlock"]   = [L("leather"), L("dagger", 2), P("Sickle"), P("Arcane Focus"), P("Scholar's Pack")],
        ["wizard"]    = [L("quarterstaff"), L("dagger", 2), P("Spellbook"), P("Arcane Focus"), P("Scholar's Pack")],
    };
}
