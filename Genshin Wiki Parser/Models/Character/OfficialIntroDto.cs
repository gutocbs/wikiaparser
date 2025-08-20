using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Character;

public sealed class OfficialIntroDto
{
    public string? Title { get; set; }
    public string? Link { get; set; }
    [JsonIgnore]
    public string? Character { get; set; }
}