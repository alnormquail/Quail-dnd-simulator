namespace CopilotTest.Services;

public enum SeatRole { Unclaimed, Dm, Player, Spectator }

/// <summary>
/// This browser session's identity at the table (Blazor circuit-scoped). Held in
/// memory for the life of the circuit; a full page reload returns to the lobby to
/// re-pick. (Persisting across reloads is a future enhancement.)
/// </summary>
public class PlayerSeat
{
    public SeatRole Role { get; private set; } = SeatRole.Unclaimed;
    public Guid? CharacterId { get; private set; }
    public string Label { get; private set; } = "";

    public bool IsUnclaimed => Role == SeatRole.Unclaimed;

    public void SetDm()
    {
        Role = SeatRole.Dm; CharacterId = null; Label = "DM";
    }

    public void SetPlayer(Guid characterId, string label)
    {
        Role = SeatRole.Player; CharacterId = characterId; Label = label;
    }

    public void SetSpectator()
    {
        Role = SeatRole.Spectator; CharacterId = null; Label = "Spectator";
    }

    public void Clear()
    {
        Role = SeatRole.Unclaimed; CharacterId = null; Label = "";
    }
}
