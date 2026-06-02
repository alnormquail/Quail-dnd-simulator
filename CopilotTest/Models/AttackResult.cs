namespace CopilotTest.Models;

public class AttackResult
{
    public bool Hit { get; set; }
    public bool CriticalHit { get; set; }
    public bool CriticalMiss { get; set; }
    public int AttackRoll { get; set; }
    public int TotalAttackRoll { get; set; }
    public int DamageRoll { get; set; }
    public int TotalDamage { get; set; }
    public string DiceRolled { get; set; } = string.Empty;
}
