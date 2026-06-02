namespace CopilotTest.Models;

public class MonsterSummary
{
    public string Index { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class MonsterDetail
{
    public string Index { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Hit_Points { get; set; }
    public string Hit_Dice { get; set; } = string.Empty;
    public int Armor_Class_Value { get; set; }
    public List<ArmorClassEntry> Armor_Class { get; set; } = new();
    public int Speed_Walk { get; set; }
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Charisma { get; set; } = 10;
    public double Challenge_Rating { get; set; }
    public int Proficiency_Bonus { get; set; } = 2;
    public List<MonsterAction> Actions { get; set; } = new();
    public string Type { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Alignment { get; set; } = string.Empty;

    public int GetArmorClass()
    {
        if (Armor_Class.Count > 0) return Armor_Class[0].Value;
        return Armor_Class_Value > 0 ? Armor_Class_Value : 10;
    }
}

public class ArmorClassEntry
{
    public int Value { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class MonsterAction
{
    public string Name { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public List<AttackBonus> Attack_Bonus_List { get; set; } = new();
    public List<DamageEntry> Damage { get; set; } = new();
    public int? Attack_Bonus { get; set; }
}

public class AttackBonus
{
    public int Value { get; set; }
}

public class DamageEntry
{
    public DamageDiceInfo Damage_Dice { get; set; } = new();
    public DamageTypeInfo? Damage_Type { get; set; }
}

public class DamageDiceInfo
{
    public string Value { get; set; } = string.Empty;
    // The API returns damage_dice as a string directly
    public static implicit operator string(DamageDiceInfo d) => d.Value;
}

public class DamageTypeInfo
{
    public string Index { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
