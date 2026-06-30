namespace CopilotTest.Models;

/// <summary>
/// A single-row store (Id is always 1) holding the live encounter serialized as JSON,
/// so an in-progress fight survives an app restart. Written by CombatEngineService after
/// each change and loaded back on startup.
/// </summary>
public class CombatSnapshot
{
    public int Id { get; set; } = 1;
    public string Json { get; set; } = string.Empty;
}
