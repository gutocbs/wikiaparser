namespace Genshin.Wiki.Parser.Models.Character;

public sealed record CharacterDto : BaseDto
{
    public string? Type { get; set; }
    public PlayableCharacterInformationDto? PlayableCharacterInformation { get; set; }
    public string? name { get; set; }
    public CharacterInformationDto? CharacterInformation { get; set; }
    public string? title { get; set; }
    public TitlesDto? Titles { get; set; }
    public string? ancestry { get; set; }
    public FamilyDto? Family { get; set; }
    public string? Description { get; set; }
}