using CopilotTest.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Tokens;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Content;
using System.Text.RegularExpressions;

namespace CopilotTest.Services;

/// <summary>
/// Parses D&D Beyond character sheet PDFs by reading AcroForm field values directly.
/// Field names are stable across D&D Beyond exports (e.g. "CharacterName", "STR", "MaxHP").
/// </summary>
public class PdfImportService
{
    public Combatant? ParseCharacterSheet(byte[] pdfBytes)
    {
        try
        {
            using var doc = PdfDocument.Open(pdfBytes);

            // Detect format: AcroForm fields → old-style fillable PDF
            // No fields → newer D&D Beyond text-layout export
            var fields = ReadAllFields(doc);
            if (fields.Count > 0)
                return BuildCombatant(fields);

            return ParseTextLayout(doc);
        }
        catch
        {
            return null;
        }
    }

    // -- Read every /T => /V pair from indirect objects
    private static Dictionary<string, string> ReadAllFields(PdfDocument doc)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tName = NameToken.Create("T");
        var vName = NameToken.Create("V");

        foreach (var kvp in doc.Structure.CrossReferenceTable.ObjectOffsets)
        {
            try
            {
                var obj = doc.Structure.GetObject(
                    new IndirectReference(kvp.Key.ObjectNumber, kvp.Key.Generation));

                if (obj?.Data is not DictionaryToken dict) continue;
                if (!dict.TryGet(tName, out IToken? tToken)) continue;
                if (!dict.TryGet(vName, out IToken? vToken)) continue;

                var key   = StripParens(tToken!.ToString()).Trim();
                var value = vToken is HexToken hex
                    ? hex.ToString()
                    : StripParens(vToken!.ToString());

                // Store both trimmed and original key so trailing-space variants match
                if (!string.IsNullOrWhiteSpace(key))
                    result[key] = value ?? "";
            }
            catch { }
        }
        return result;
    }

    // -- Parser for newer D&D Beyond text-layout PDFs (no AcroForm fields)
    // Words are extracted with bounding-box positions; we reconstruct data by proximity.
    private static Combatant? ParseTextLayout(PdfDocument doc)
    {
        var c = new Combatant { Type = CombatantType.PC, Actions = new List<CombatAction>() };

        // Collect all words from page 1 with their positions
        var page = doc.GetPages().FirstOrDefault();
        if (page == null) return null;

        // Build a list of (x, y, text) sorted top-to-bottom, left-to-right
        var words = page.GetWords()
            .Select(w => (x: w.BoundingBox.Left, y: w.BoundingBox.Top, t: w.Text))
            .OrderByDescending(w => w.y)
            .ThenBy(w => w.x)
            .ToList();

        // Helper: get all words in a Y band (±tolerance) and optional X range
        _ = (Func<double, double, double, double, List<string>>)((yCenter, yTol, xMin, xMax) =>
            words.Where(w => Math.Abs(w.y - yCenter) <= yTol && w.x >= xMin && w.x <= xMax)
                 .OrderBy(w => w.x).Select(w => w.t).ToList());

        // Helper: find nearest word to a given (x,y) within tolerances
        string Near(double x, double y, double xTol, double yTol) =>
            words.Where(w => Math.Abs(w.x - x) <= xTol && Math.Abs(w.y - y) <= yTol)
                 .OrderBy(w => Math.Abs(w.x - x) + Math.Abs(w.y - y))
                 .Select(w => w.t).FirstOrDefault() ?? "";

        // Helper: find nearest NUMBER to a given (x,y)
        int NearInt(double x, double y, double xTol, double yTol, int fallback = 0)
        {
            var t = Near(x, y, xTol, yTol);
            return int.TryParse(t.TrimStart('+'), out var v) ? v : fallback;
        }

        // ── Character name ─────────────────────────────────────────────────
        // Name is near Y=740, left side, 2 consecutive words
        var nameWords = words.Where(w => w.y is >= 735 and <= 746 && w.x < 200)
                             .OrderBy(w => w.x).ToList();
        c.Name = nameWords.Count > 0
            ? string.Join(" ", nameWords.Select(w => w.t))
            : "Imported Character";

        // ── Ability scores ──────────────────────────────────────────────────
        // Labels (STRENGTH, DEXTERITY...) at Y≈692, values at Y≈664
        // Ordered by X: STR≈63, DEX≈102, CON≈143, INT≈184, WIS≈225, CHA≈267
        var statValues = words
            .Where(w => w.y is >= 660 and <= 668)
            .OrderBy(w => w.x)
            .Where(w => int.TryParse(w.t, out _))
            .Select(w => int.Parse(w.t))
            .ToList();

        if (statValues.Count >= 6)
        {
            c.Strength     = statValues[0];
            c.Dexterity    = statValues[1];
            c.Constitution = statValues[2];
            c.Intelligence = statValues[3];
            c.Wisdom       = statValues[4];
            c.Charisma     = statValues[5];
        }
        else
        {
            // Fallback: look near exact X positions for each stat
            // Stat score X positions (approximate from dump): 64, 103, 144, 185, 226, 268
            double[] statX = { 64, 103, 144, 185, 226, 268 };
            c.Strength     = NearInt(statX[0], 664, 12, 6, 10);
            c.Dexterity    = NearInt(statX[1], 664, 12, 6, 10);
            c.Constitution = NearInt(statX[2], 664, 12, 6, 10);
            c.Intelligence = NearInt(statX[3], 664, 12, 6, 10);
            c.Wisdom       = NearInt(statX[4], 664, 12, 6, 10);
            c.Charisma     = NearInt(statX[5], 664, 12, 6, 10);
        }

        // ── HP ─────────────────────────────────────────────────────────────
        // Pattern "56 / 56" at Y≈676; grab Max HP (second number)
        var hpWords = words.Where(w => w.y is >= 672 and <= 680 && w.x >= 475 && w.x <= 560)
                           .OrderBy(w => w.x).Select(w => w.t).ToList();
        // hpWords = ["56", "/", "56", "--"] → first number = current, third = max
        var hpNums = hpWords.Where(t => int.TryParse(t, out _)).Select(int.Parse).ToList();
        c.MaxHitPoints     = hpNums.Count >= 2 ? hpNums[1] : (hpNums.Count == 1 ? hpNums[0] : 10);
        c.CurrentHitPoints = hpNums.Count >= 1 ? hpNums[0] : c.MaxHitPoints;

        // ── AC ─────────────────────────────────────────────────────────────
        // AC number near Y≈629, X≈344
        c.ArmorClass = NearInt(344, 629, 20, 8, 10);
        if (c.ArmorClass == 0) c.ArmorClass = 10;

        // ── Speed ──────────────────────────────────────────────────────────
        // Speed number near Y≈675, X≈357 (before "ft.")
        c.Speed = NearInt(357, 675, 20, 6, 30);
        if (c.Speed == 0) c.Speed = 30;

        // ── Proficiency bonus ──────────────────────────────────────────────
        // Near Y≈674, X≈316
        c.ProficiencyBonus = NearInt(316, 674, 15, 6, 2);
        if (c.ProficiencyBonus == 0) c.ProficiencyBonus = 2;

        // ── Attacks ────────────────────────────────────────────────────────
        // Attack rows are in the right half. Layout columns (approx):
        //   Name      X ≈ 310–370
        //   Range     X ≈ 380–405
        //   Hit/DC    X ≈ 415–425
        //   Damage    X ≈ 438–460
        //
        // Strategy: find every Y that has a dice expression in the damage column,
        // then back-fill name/hit/range from the same Y band (±5pt).
        // A second sub-row at Y-14 may carry "Melee Weapon / Reach" — ignore those.

        var damageWords = words
            .Where(w => w.x >= 430 && w.x <= 470 && w.y > 280 && w.y < 560
                        && Regex.IsMatch(w.t, @"^\d+d\d+"))
            .OrderByDescending(w => w.y)
            .ToList();

        var seenAttackNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dmgWord in damageWords)
        {
            double rowY = dmgWord.y;
            string dmgToken = dmgWord.t;

            // Name: words at X 305–375 within ±5pt of rowY
            var nameParts = words
                .Where(w => Math.Abs(w.y - rowY) <= 5 && w.x >= 305 && w.x < 375)
                .OrderBy(w => w.x).Select(w => w.t).ToList();

            if (nameParts.Count == 0) continue;
            var nameText = string.Join(" ", nameParts);

            // Skip if it's a column header or sub-row descriptor
            if (nameText is "ATTACK" or "Melee" or "Melee Weapon" or "Melee Attack" or "Ranged" or "Ranged Weapon") continue;
            if (seenAttackNames.Contains(nameText)) continue;
            seenAttackNames.Add(nameText);

            // Hit bonus: number near X 415–430, same Y band
            var hitToken = words
                .Where(w => Math.Abs(w.y - rowY) <= 5 && w.x >= 412 && w.x <= 432)
                .OrderBy(w => Math.Abs(w.x - 420)).Select(w => w.t).FirstOrDefault() ?? "";
            int hitBonus = int.TryParse(hitToken.TrimStart('+'), out var hv) ? hv : 0;

            // Range: number near X 380–400, same Y band
            var rangeNum = words
                .Where(w => Math.Abs(w.y - rowY) <= 8 && w.x >= 375 && w.x <= 405)
                .Where(w => Regex.IsMatch(w.t, @"^\d"))
                .OrderBy(w => w.x).Select(w => w.t).FirstOrDefault() ?? "";
            // Also check sub-row for "Reach" or "(60)" etc.
            var rangeExtra = words
                .Where(w => w.y >= rowY - 20 && w.y <= rowY + 5 && w.x >= 375 && w.x <= 415)
                .Select(w => w.t).FirstOrDefault(t => t is "Reach" or "Touch") ?? "";
            string range = rangeNum != "" ? $"{rangeNum} ft." : (rangeExtra != "" ? rangeExtra : "5 ft.");

            var (dice, dmgBonus, dmgType) = ParseDamage(dmgToken);

            c.Actions.Add(new CombatAction
            {
                Name        = nameText,
                ActionType  = ActionType.Attack,
                AttackBonus = hitBonus,
                DamageDice  = dice,
                DamageBonus = dmgBonus,
                DamageType  = dmgType,
                Range       = range
            });
        }

        // If no attacks parsed, add a default unarmed strike
        if (c.Actions.Count == 0)
        {
            c.Actions.Add(new CombatAction
            {
                Name        = "Unarmed Strike",
                ActionType  = ActionType.Attack,
                AttackBonus = Combatant.GetModifier(c.Strength) + c.ProficiencyBonus,
                DamageDice  = "1",
                DamageBonus = Combatant.GetModifier(c.Strength)
            });
        }

        return c;
    }

    // -- Build Combatant from field map
    private static Combatant BuildCombatant(Dictionary<string, string> f)
    {
        var c = new Combatant { Type = CombatantType.PC, Actions = new List<CombatAction>() };

        c.Name             = Get(f, "CharacterName", "Imported Character");
        c.ProficiencyBonus = ParseBonus(Get(f, "ProfBonus")) is int pb && pb != 0 ? pb : 2;
        c.MaxHitPoints     = ParseInt(Get(f, "MaxHP"), 10);
        if (c.MaxHitPoints == 0) c.MaxHitPoints = 10;
        c.CurrentHitPoints = c.MaxHitPoints;
        c.ArmorClass       = ParseInt(Get(f, "AC"), 10);
        if (c.ArmorClass == 0) c.ArmorClass = 10;
        c.Speed            = ParseSpeedFt(Get(f, "Speed"), 30);

        c.Strength     = ParseInt(Get(f, "STR"), 10);
        c.Dexterity    = ParseInt(Get(f, "DEX"), 10);
        c.Constitution = ParseInt(Get(f, "CON"), 10);
        c.Intelligence = ParseInt(Get(f, "INT"), 10);
        c.Wisdom       = ParseInt(Get(f, "WIS"), 10);
        c.Charisma     = ParseInt(Get(f, "CHA"), 10);

        // ── Class and Level ──────────────────────────────────────────────
        var classLevel = Get(f, "CLASS  LEVEL");
        var clMatch = Regex.Match(classLevel, @"^([A-Za-z /]+?)\s+(\d+)$");
        if (clMatch.Success)
        {
            c.CharacterClass = clMatch.Groups[1].Value.Trim();
            c.CharacterLevel = int.Parse(clMatch.Groups[2].Value);
        }
        else if (!string.IsNullOrWhiteSpace(classLevel))
        {
            c.CharacterClass = classLevel.Trim();
        }

        // ── Barbarian Rage ──────────────────────────────────────────────
        // Detect "Barbarian N" in CLASS  LEVEL field and populate rage stats.
        var barbMatch  = Regex.Match(classLevel, @"Barbarian\s+(\d+)", RegexOptions.IgnoreCase);
        if (barbMatch.Success)
        {
            int barbLevel = int.Parse(barbMatch.Groups[1].Value);
            c.IsBarbarianClass   = true;
            c.RageBonus          = barbLevel >= 16 ? 4 : barbLevel >= 9 ? 3 : 2;
            c.RageUsesPerDay     = barbLevel >= 20 ? 99  // unlimited at 20
                                 : barbLevel >= 17 ? 6
                                 : barbLevel >= 12 ? 5
                                 : barbLevel >= 8  ? 4
                                 : barbLevel >= 6  ? 3
                                 : 2; // levels 1-5
            c.RageUsesRemaining  = c.RageUsesPerDay;
        }

        // Weapon attacks: slot 0 uses "Wpn Name"/"Wpn1 AtkBonus"/"Wpn1 Damage"
        // Slots 1+ use "Wpn Name 2"/"Wpn2 AtkBonus"/"Wpn2 Damage" etc.
        // Note: field names may have trailing spaces — keys are pre-trimmed in ReadAllFields.
        var seenWeapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < 6; i++)
        {
            string nameKey = i == 0 ? "Wpn Name"      : $"Wpn Name {i + 1}";
            string atkKey  = i == 0 ? "Wpn1 AtkBonus" : $"Wpn{i + 1} AtkBonus";
            string dmgKey  = i == 0 ? "Wpn1 Damage"   : $"Wpn{i + 1} Damage";

            var wpnName = Get(f, nameKey).Trim();
            if (string.IsNullOrWhiteSpace(wpnName)) continue; // don't break; check all slots
            if (seenWeapons.Contains(wpnName)) continue;
            seenWeapons.Add(wpnName);

            var rawDmgStr = Get(f, dmgKey).Trim();
            // Skip weapons with 0 damage (e.g. "0 Bludgeoning" = placeholder Unarmed Strike)
            var zeroCheck = Regex.Match(rawDmgStr, @"^(\d+)");
            if (zeroCheck.Success && int.Parse(zeroCheck.Groups[1].Value) == 0 && !rawDmgStr.Contains('d')) continue;

            var (dice, bonus, dmgType) = ParseDamage(rawDmgStr);
            c.Actions.Add(new CombatAction
            {
                Name        = wpnName,
                ActionType  = ActionType.Attack,
                AttackBonus = ParseBonus(Get(f, atkKey)),
                DamageDice  = dice,
                DamageBonus = bonus,
                DamageType  = dmgType,
                Range       = "5 ft"
            });
        }

        // Spells: build a sorted list of (firstSpellIndex, level) from spellHeader fields,
        // then assign each spellName to the correct level by finding the closest preceding header.
        //
        // D&D Beyond layout:
        //   spellHeader0  = "=== CANTRIPS ===" (index 0)
        //   spellName0..3 = cantrip spells
        //   spellHeader1  = "=== 1st LEVEL ===" + spellSlotHeader1 = "4 Slots OOOO"
        //   spellName4..8 = 1st level spells ... and so on.

        var spellSaveDC   = ParseInt(Get(f, "spellSaveDC0"), 13);
        var spellAtkBonus = ParseBonus(Get(f, "spellAtkBonus0"));

        // Build level → slot count from spellSlotHeader{n}
        var slotsPerLevel = new Dictionary<int, int>();
        for (int h = 0; h < 20; h++)
        {
            var slotHdr = Get(f, $"spellSlotHeader{h}");
            if (string.IsNullOrWhiteSpace(slotHdr)) continue;
            var hdr = Get(f, $"spellHeader{h}");
            int lvl = ParseSpellLevel(hdr);
            var slotMatch = Regex.Match(slotHdr, @"(\d+)\s+Slot");
            if (slotMatch.Success && lvl > 0)
                slotsPerLevel[lvl] = int.Parse(slotMatch.Groups[1].Value);
        }

        // Gather (headerFieldIndex → level) for spell range grouping
        var levelBreaks = new SortedList<int, int>(); // key=field index, value=spell level
        for (int h = 0; h < 20; h++)
        {
            var hdr = Get(f, $"spellHeader{h}");
            if (string.IsNullOrWhiteSpace(hdr)) continue;
            levelBreaks[h] = ParseSpellLevel(hdr);
        }

        // Build a lookup of weapon names already added (to avoid duplicating them as spells)
        var weaponNames = new HashSet<string>(
            c.Actions.Select(a => a.Name), StringComparer.OrdinalIgnoreCase);

        // Build a weapon-table lookup for dice/bonus by spell name
        // (e.g. "Shocking Grasp" appears in both weapon table and spell list;
        //  prefer weapon table stats which are per-character)
        var weaponStatsByName = c.Actions.ToDictionary(
            a => a.Name, a => (a.DamageDice, a.DamageBonus, a.AttackBonus),
            StringComparer.OrdinalIgnoreCase);

        // Walk spellName fields; stop when we hit 5 consecutive missing names
        int misses = 0;
        for (int i = 0; i < 100; i++)
        {
            var spellName = Regex.Replace(Get(f, $"spellName{i}").Trim(), @"\s*\[R\]", "", RegexOptions.IgnoreCase).Trim();
            if (string.IsNullOrWhiteSpace(spellName))
            {
                if (++misses >= 5) break;
                continue;
            }
            misses = 0;

            // Find the level: the highest header index that is <= i
            int level = 0;
            foreach (var lb in levelBreaks)
            {
                if (lb.Key <= i) level = lb.Value;
                else break;
            }

            var saveHit = Get(f, $"spellSaveHit{i}").Trim();
            bool isSave = Regex.IsMatch(saveHit, @"^[A-Z]{3}\s+\d+$");
            bool isAtk  = saveHit.StartsWith("+") || saveHit.StartsWith("-");
            bool isNone = saveHit == "--" || string.IsNullOrWhiteSpace(saveHit);

            // Skip purely utility spells (no attack, no save, no damage in weapon table)
            if (isNone && !weaponStatsByName.ContainsKey(spellName)) continue;

            // Skip if this spell is already in the weapon table (weapon stats are more precise)
            if (weaponNames.Contains(spellName)) continue;

            ActionType aType = level == 0 ? ActionType.SpellAttack
                             : isSave     ? ActionType.Spell
                             : isAtk      ? ActionType.SpellAttack
                             :              ActionType.Spell;

            int saveDcOverride       = 0;
            AbilityScore saveAbility = AbilityScore.Dexterity;
            if (isSave)
            {
                var parts      = saveHit.Split(' ');
                saveAbility    = ParseAbility(parts[0]);
                saveDcOverride = ParseInt(parts.Length > 1 ? parts[1] : "", spellSaveDC);
            }

            // Default dice by level; override from weapon table if present
            var dmgDice  = level switch
            {
                0 => "1d10", 1 => "2d6", 2 => "3d6", 3 => "4d8",
                4 => "5d8",  5 => "6d10", 6 => "7d10", 7 => "8d10",
                8 => "9d10", _ => "10d10"
            };
            int dmgBonus = 0;
            int atkBonus = isAtk ? ParseBonus(saveHit) : spellAtkBonus;
            if (weaponStatsByName.TryGetValue(spellName, out var ws))
            {
                dmgDice  = ws.DamageDice;
                dmgBonus = ws.DamageBonus;
                atkBonus = ws.AttackBonus;
            }

            // UsesPerDay: 0 = unlimited (cantrip); leveled spells share a pool per level
            // We store UsesPerDay per-action equal to the slot count for that level.
            int usesPerDay = slotsPerLevel.TryGetValue(level, out var slots) ? slots : 0;

            var source   = Get(f, $"spellSource{i}");
            var duration = Get(f, $"spellDuration{i}");
            var desc     = string.Join(" — ", new[] { source, duration }.Where(s => !string.IsNullOrWhiteSpace(s)));

            c.Actions.Add(new CombatAction
            {
                Name        = spellName,
                ActionType  = aType,
                SpellLevel  = level,
                DamageDice  = dmgDice,
                DamageBonus = dmgBonus,
                SaveDC      = saveDcOverride > 0 ? saveDcOverride : spellSaveDC,
                SaveAbility = saveAbility,
                AttackBonus = atkBonus,
                Range       = Get(f, $"spellRange{i}"),
                Description = desc,
                UsesPerDay  = usesPerDay,
                UsesRemaining = usesPerDay
            });
        }

        if (c.Actions.Count == 0)
        {
            c.Actions.Add(new CombatAction
            {
                Name        = "Attack",
                ActionType  = ActionType.Attack,
                AttackBonus = Combatant.GetModifier(c.Strength) + c.ProficiencyBonus,
                DamageDice  = "1d4",
                DamageBonus = Combatant.GetModifier(c.Strength)
            });
        }


        return c;
    }

    private static int ParseSpellLevel(string header)
    {
        if (header.Contains("CANTRI", StringComparison.OrdinalIgnoreCase)) return 0;
        var m = Regex.Match(header, @"(\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    // -- Helpers
    private static string Get(Dictionary<string, string> f, string key, string fallback = "")
        => f.TryGetValue(key, out var v) ? v : fallback;

    private static string StripParens(string? s)
    {
        if (s == null) return "";
        s = s.Trim();
        if (s.StartsWith('(') && s.EndsWith(')'))
            s = s[1..^1];
        return s.Trim();
    }

    private static int ParseInt(string s, int fallback = 0)
        => int.TryParse(s.Trim().TrimStart('+'), out var v) ? v : fallback;

    private static int ParseBonus(string s)
    {
        s = s.Trim();
        if (s.StartsWith('+')) s = s[1..];
        return int.TryParse(s, out var v) ? v : 0;
    }

    private static int ParseSpeedFt(string s, int fallback)
    {
        var m = Regex.Match(s, @"(\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value) : fallback;
    }

    private static (string dice, int bonus, DamageType type) ParseDamage(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return ("1d4", 0, DamageType.None);

        var m = Regex.Match(s, @"(\d+d\d+)([+\-]\d+)?\s*(\w+)?", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            return (m.Groups[1].Value,
                    m.Groups[2].Success ? ParseBonus(m.Groups[2].Value) : 0,
                    ParseDamageType(m.Groups[3].Value));
        }

        // Flat damage: "3 Bludgeoning"
        var m2 = Regex.Match(s, @"(\d+)\s*(\w+)?");
        if (m2.Success)
            return ("1", int.Parse(m2.Groups[1].Value), ParseDamageType(m2.Groups[2].Value));

        return ("1d4", 0, DamageType.None);
    }

    private static DamageType ParseDamageType(string s) => s?.ToLower() switch
    {
        "slashing"    => DamageType.Slashing,
        "piercing"    => DamageType.Piercing,
        "bludgeoning" => DamageType.Bludgeoning,
        "fire"        => DamageType.Fire,
        "cold"        => DamageType.Cold,
        "lightning"   => DamageType.Lightning,
        "thunder"     => DamageType.Thunder,
        "acid"        => DamageType.Acid,
        "poison"      => DamageType.Poison,
        "necrotic"    => DamageType.Necrotic,
        "radiant"     => DamageType.Radiant,
        "psychic"     => DamageType.Psychic,
        "force"       => DamageType.Force,
        _             => DamageType.None
    };

    private static AbilityScore ParseAbility(string s) => s?.ToUpper() switch
    {
        "STR" => AbilityScore.Strength,
        "DEX" => AbilityScore.Dexterity,
        "CON" => AbilityScore.Constitution,
        "INT" => AbilityScore.Intelligence,
        "WIS" => AbilityScore.Wisdom,
        "CHA" => AbilityScore.Charisma,
        _     => AbilityScore.Dexterity
    };
}
