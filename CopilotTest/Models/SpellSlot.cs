namespace CopilotTest.Models;

public class SpellSlot
{
    public int Id { get; set; }
    public Guid CharacterId { get; set; }
    public int Level { get; set; }
    public int MaxSlots { get; set; }
    public int UsedSlots { get; set; } = 0;
    public int RemainingSlots => MaxSlots - UsedSlots;
}
