using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Character;

public sealed class CharacterStoryDto
{
    public string? Title { get; set; }
    public string? Text { get; set; }
    [JsonIgnore]
    public int? Friendship { get; set; }
    [JsonIgnore]
    public List<string>? Mentions { get; set; }
}