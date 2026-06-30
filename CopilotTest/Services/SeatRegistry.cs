namespace CopilotTest.Services;

/// <summary>
/// Shared, server-wide registry of who has "claimed" which seat at the table —
/// the DM seat and which player controls which character. Used by the lobby so
/// two devices can't grab the same character and everyone sees who's connected.
/// In-memory and singleton; raises <see cref="OnChanged"/> so lobbies refresh live.
/// </summary>
public class SeatRegistry
{
    private readonly object _gate = new();
    private bool _dmTaken;
    private readonly Dictionary<Guid, string> _claimed = new();   // characterId -> display label

    public event Action? OnChanged;

    public bool DmTaken { get { lock (_gate) { return _dmTaken; } } }

    public bool IsClaimed(Guid characterId)
    {
        lock (_gate) { return _claimed.ContainsKey(characterId); }
    }

    public IReadOnlyDictionary<Guid, string> Claims()
    {
        lock (_gate) { return new Dictionary<Guid, string>(_claimed); }
    }

    public void ClaimDm()
    {
        lock (_gate) { _dmTaken = true; }
        OnChanged?.Invoke();
    }

    public void ReleaseDm()
    {
        lock (_gate) { _dmTaken = false; }
        OnChanged?.Invoke();
    }

    /// <summary>
    /// Claim a character seat. Always succeeds — a "taken" seat can be re-claimed, so a
    /// player whose tab closed (leaving a stale claim) can take their character back, and
    /// a saved seat can be restored on reload. Trusted-group app; no real ownership.
    /// </summary>
    public void ClaimCharacter(Guid characterId, string label)
    {
        lock (_gate) { _claimed[characterId] = label; }
        OnChanged?.Invoke();
    }

    public void ReleaseCharacter(Guid characterId)
    {
        lock (_gate) { _claimed.Remove(characterId); }
        OnChanged?.Invoke();
    }
}
