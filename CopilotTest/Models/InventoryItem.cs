namespace CopilotTest.Models;

public enum ItemCategory { Weapon, Armor, Consumable, Tool, Treasure, Other }

public class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string Weight { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEquipped { get; set; } = false;
    public ItemCategory Category { get; set; } = ItemCategory.Other;
}
