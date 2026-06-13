namespace CopilotTest.Services;

public enum RollKind { Check, Save, Attack, Damage, Initiative, Other }

public record RollEntry(
    DateTime At,
    string Who,
    string Label,
    string Detail,
    int Total,
    RollKind Kind,
    bool Crit,
    bool Fumble);

/// <summary>
/// Per-session in-memory log of recent dice rolls. Registered as Scoped, so each
/// Blazor circuit (browser tab) keeps its own roll history for the play session.
/// </summary>
public class RollLogService
{
    private readonly List<RollEntry> _entries = new();
    private const int MaxEntries = 30;

    public IReadOnlyList<RollEntry> Entries => _entries;
    public event Action? Changed;

    public void Add(RollEntry entry)
    {
        _entries.Insert(0, entry);
        if (_entries.Count > MaxEntries) _entries.RemoveAt(_entries.Count - 1);
        Changed?.Invoke();
    }

    public void Clear()
    {
        _entries.Clear();
        Changed?.Invoke();
    }
}
