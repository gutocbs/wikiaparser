using Genshin.Wiki.Parser.Enum;
using Genshin.Wiki.Parser.Models.Quest;

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
    public bool ShouldSerializeLocations() => Locations?.Count > 0;
    public List<string>? Affiliations { get; set; }   // Narzissenkreuz Ordo, etc.
    public bool ShouldSerializeAffiliations() => Affiliations?.Count > 0;
    public string? Deceased { get; set; }             // texto livre / “Past” etc.
    public List<DetailDto>? Family { get; set; }         // father/mother/spouse/...
    public bool ShouldSerializeFamily() => Family?.Count > 0;
    public string? ShortDescription { get; set; }     // {{Description|...}}
    public string? Profile { get; set; }              // seção ==Profile==
    public bool ShouldSerializeProfile() => !string.IsNullOrWhiteSpace(Profile) && !Profile.Contains("(To be added.)");
    
    public string? Appearance { get; set; }           // seção ==Appearance== (se houver)
    public bool ShouldSerializeAppearance() => !string.IsNullOrWhiteSpace(Appearance) && !Appearance.Contains("(To be added.)");
    // Diálogo (estrutura leve)
    public List<DialogueSection> Dialogues { get; set; } = new();
    public bool ShouldSerializeDialogues() => Dialogues.Count > 0;
}
