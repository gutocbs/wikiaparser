using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Character;

public sealed class VoCueDto
{
    [JsonIgnore]
    public int Index { get; set; }            // 1,2,3...
    public string? Text { get; set; }
    [JsonIgnore]
    public List<string>? Files { get; set; }
}