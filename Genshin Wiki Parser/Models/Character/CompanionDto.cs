using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Character;

public sealed class CompanionDto
{
    [JsonIgnore]
    public string? Character { get; set; }
    public List<CompanionQuoteDto>? IdleQuotes { get; set; }
    public List<CompanionDialogueScenarioDto>? Dialogues { get; set; } // inclui principal e “Special Dialogue”
}