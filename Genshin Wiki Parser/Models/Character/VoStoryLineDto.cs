using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Character;

public sealed class VoStoryLineDto
{
    [JsonIgnore]
    public string? Key { get; set; }          // ex.: "vo_01_01"
    public string? Title { get; set; }        // "Hello", "Chat: Rules", etc.
    public string? Text { get; set; }
    public int? Friendship { get; set; }
    public int? Ascension { get; set; }
    public string? Quest { get; set; }
    public List<string>? Mentions { get; set; }

    public bool ShouldSerializeTitle()
    {
        Title = Title?.Replace("{character}", "Player").Replace("{name}", "Player");
        Text = Text?.Replace("{character}", "Player").Replace("{name}", "Player");
        
        return true;
    }
}