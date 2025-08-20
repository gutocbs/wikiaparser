using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Character;

public sealed class VoiceOversDto
{
    [JsonIgnore]
    public string? Character { get; set; }
    public List<VoStoryLineDto>? Story { get; set; }
    [JsonIgnore]
    public Dictionary<string, List<VoCueDto>>? Combat { get; set; }
}