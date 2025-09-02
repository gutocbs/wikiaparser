using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Character;

public sealed class VoStoryLineDto
{
    [JsonIgnore]
    public string? Key { get; set; }          // ex.: "vo_01_01"
    public string? Title { get; set; }        // "Hello", "Chat: Rules", etc.
    public string? Text { get; set; }
    public int Friendship { get; set; }
    public bool ShouldSerializeFriendship() => Friendship > 0;
    public int? Ascension { get; set; }
    public bool ShouldSerializeAscension() => Ascension > 0;
    public string? Quest { get; set; }
    public bool ShouldSerializeQuest() => !string.IsNullOrWhiteSpace(Quest);
    public List<string>? Mentions { get; set; }
    public bool ShouldSerializeMentions() => Mentions?.Count > 0;
}