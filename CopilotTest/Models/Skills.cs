namespace CopilotTest.Models;

public enum Skill
{
    Acrobatics, AnimalHandling, Arcana, Athletics, Deception,
    History, Insight, Intimidation, Investigation, Medicine,
    Nature, Perception, Performance, Persuasion, Religion,
    SleightOfHand, Stealth, Survival
}

public enum ProficiencyLevel { None, Proficient, Expertise }

public static class SkillAbilityMap
{
    public static readonly Dictionary<Skill, AbilityScore> Map = new()
    {
        { Skill.Acrobatics,    AbilityScore.Dexterity    },
        { Skill.AnimalHandling,AbilityScore.Wisdom        },
        { Skill.Arcana,        AbilityScore.Intelligence  },
        { Skill.Athletics,     AbilityScore.Strength      },
        { Skill.Deception,     AbilityScore.Charisma      },
        { Skill.History,       AbilityScore.Intelligence  },
        { Skill.Insight,       AbilityScore.Wisdom        },
        { Skill.Intimidation,  AbilityScore.Charisma      },
        { Skill.Investigation, AbilityScore.Intelligence  },
        { Skill.Medicine,      AbilityScore.Wisdom        },
        { Skill.Nature,        AbilityScore.Intelligence  },
        { Skill.Perception,    AbilityScore.Wisdom        },
        { Skill.Performance,   AbilityScore.Charisma      },
        { Skill.Persuasion,    AbilityScore.Charisma      },
        { Skill.Religion,      AbilityScore.Intelligence  },
        { Skill.SleightOfHand, AbilityScore.Dexterity     },
        { Skill.Stealth,       AbilityScore.Dexterity     },
        { Skill.Survival,      AbilityScore.Wisdom        },
    };

    public static string DisplayName(Skill s) => s switch
    {
        Skill.AnimalHandling => "Animal Handling",
        Skill.SleightOfHand  => "Sleight of Hand",
        _                    => s.ToString()
    };
}
