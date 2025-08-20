using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Character;

public sealed class CharacterInformationDto
{
    public string? RealName { get; set; }
    public string? Birthday { get; set; }
    public string? Constellation { get; set; }
    public List<string?>? Regions { get; set; }
    public List<string?>? Affiliations { get; set; }
    public string? Dish { get; set; }
    public string? Namecard { get; set; }
    public string? ObtainType { get; set; }
    public string? Obtain { get; set; }
    [JsonIgnore]
    public string? ReleaseDate { get; set; }
}