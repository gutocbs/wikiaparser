using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Character;

public sealed class NamecardDto
{
    [JsonIgnore]
    public string? Character { get; set; }           // Faruzan
    public string? Title { get; set; }               // Faruzan: Sealed Secret
    [JsonIgnore]
    public int? Id { get; set; }                     // 210144
    public string? Description { get; set; }         // texto limpo
    public List<string>? Sources { get; set; }       // Reward for reaching
}