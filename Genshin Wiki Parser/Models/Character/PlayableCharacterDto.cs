using Genshin.Wiki.Parser.Enum;

namespace Genshin.Wiki.Parser.Models.Character;

public class PlayableCharacterDto : BaseDto
{
    public PlayableCharacterDto()
    {
        ObjectType = ObjectTypeEnum.PlayableCharacter;
    }
    
    public string? Type { get; set; }
    public PlayableCharacterInformationDto? PlayableCharacterInformation { get; set; }
    public string? Name { get; set; }
    public CharacterInformationDto? CharacterInformation { get; set; }
    public List<string?>? Titles { get; set; }
    public bool ShouldSerializeTitles() => Titles?.Count > 0;
    public string? Ancestry { get; set; }
    public List<DetailDto>? Family { get; set; }
    public bool ShouldSerializeFamily() => Family?.Count > 0;
    public string? Description { get; set; }
    public LoreDto? Lore { get; set; }
    public VoiceOversDto? VoiceOvers { get; set; }
    public CompanionDto? Companion { get; set; }
    public NamecardDto? Namecard { get; set; }
}