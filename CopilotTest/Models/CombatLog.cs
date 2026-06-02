namespace CopilotTest.Models;

public class CombatLog
{
    public int Round { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public LogEntryType EntryType { get; set; } = LogEntryType.Info;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public enum LogEntryType
{
    Info,
    Attack,
    Hit,
    Miss,
    Damage,
    Kill,
    DeathSave,
    Condition,
    RoundStart
}
