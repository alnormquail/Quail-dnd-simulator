namespace CopilotTest.Models;

public class CharacterSkill
{
    public int Id { get; set; }
    public Guid CharacterId { get; set; }
    public Skill Skill { get; set; }
    public ProficiencyLevel Proficiency { get; set; } = ProficiencyLevel.None;
}
