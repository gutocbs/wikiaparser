namespace Genshin.Wiki.Parser.Models.Character;

public sealed class CompanionDialogueScenarioDto
{
    public string? ScenarioTitle { get; set; } // null para o bloco “Dialogue” principal
    public List<string>? Conditions { get; set; } // ex.: “Unlocks at Friendship Level 4”, “Between 6:00 and 19:00”
    public List<CompanionDialogueEntryDto>? Entries { get; set; }
}