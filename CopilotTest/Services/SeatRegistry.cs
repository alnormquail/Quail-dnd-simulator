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

    /// <summary>Claim a character seat. Returns false if someone already holds it.</summary>
    public bool ClaimCharacter(Guid characterId, string label)
    {
        lock (_gate)
        {
            if (_claimed.ContainsKey(characterId)) return false;
            _claimed[characterId] = label;
        }
        OnChanged?.Invoke();
        return true;
    }

    public void ReleaseCharacter(Guid characterId)
    {
        lock (_gate) { _claimed.Remove(characterId); }
        OnChanged?.Invoke();
    }
}
