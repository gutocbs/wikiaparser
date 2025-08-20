using Genshin.Wiki.Parser.Enum;

namespace Genshin.Wiki.Parser.Models.Character;

public sealed class NpcDto : BaseDto
{
    public NpcDto()
    {
        ObjectType = ObjectTypeEnum.NonPlayableCharacter;
    }
    
    public string? Name { get; set; }                 // Caterpillar (page title)
    public string? RealName { get; set; }             // do infobox (se houver)
    public string? Type { get; set; }                 // Quest NPC, NPC, etc.
    public string? Element { get; set; }              // Anemo, Pyro, ...
    public string? Region { get; set; }               // Fontaine, Sumeru, ...
    public List<string>? Locations { get; set; }      // pode ter múltiplas no futuro
    public List<string>? Affiliations { get; set; }   // Narzissenkreuz Ordo, etc.
    public string? Deceased { get; set; }             // texto livre / “Past” etc.
    public List<DetailDto>? Family { get; set; }         // father/mother/spouse/...
    public string? ShortDescription { get; set; }     // {{Description|...}}
    public string? Profile { get; set; }              // seção ==Profile==
    public string? Appearance { get; set; }           // seção ==Appearance== (se houver)
    
    public bool ShouldSerializeProfile()
    { 
        if(!string.IsNullOrWhiteSpace(Profile) && Profile.Contains("(To be added.)"))
            Profile = null;
        if(!string.IsNullOrWhiteSpace(Appearance) && Appearance.Contains("(To be added.)"))
            Appearance = null;
        return true;
    }
}
