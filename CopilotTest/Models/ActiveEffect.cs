namespace CopilotTest.Models;

/// <summary>
/// A timed, source-tagged effect on a combatant — the foundation for spells and
/// conditions that should expire on their own instead of living forever.
///
/// An effect may impose a <see cref="Condition"/> (e.g. a spell that Restrains a
/// target); while the effect is active the condition is mirrored into the
/// combatant's <c>Conditions</c> set (the engine's source of truth), and when the
/// effect expires the condition clears with it. Effects tagged
/// <see cref="FromConcentration"/> end when the caster (<see cref="ConcentratorId"/>)
/// loses concentration.
/// </summary>
public class ActiveEffect
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name, e.g. "Hex", "Bless", "Ensnaring Strike".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Where it came from — a caster's name, "DM", etc.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>A condition this effect imposes while active (optional).</summary>
    public Condition? Condition { get; set; }

    /// <summary>Rounds left before it expires. -1 means it lasts until removed.</summary>
    public int RoundsRemaining { get; set; } = -1;

    /// <summary>True if this effect is sustained by a caster's concentration.</summary>
    public bool FromConcentration { get; set; }

    /// <summary>The caster maintaining this effect via concentration, if any.</summary>
    public Guid? ConcentratorId { get; set; }

    public bool IsIndefinite => RoundsRemaining < 0;

    /// <summary>Short label for the UI, e.g. "Hex (3r)" or "Bless".</summary>
    public string Label => IsIndefinite ? Name : $"{Name} ({RoundsRemaining}r)";
}
