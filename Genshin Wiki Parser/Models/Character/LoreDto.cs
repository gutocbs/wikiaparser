using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Character;

public sealed class LoreDto
{
    [JsonIgnore]
    public string? Character { get; set; }
    public string? SummaryQuote { get; set; }
    public List<QuoteDto>? Quotes { get; set; }
    public string? Personality { get; set; }
    public string? Appearance { get; set; }
    public OfficialIntroDto? OfficialIntroduction { get; set; }
    public List<CharacterStoryDto>? CharacterStories { get; set; }
}