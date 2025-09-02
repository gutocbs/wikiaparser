using Genshin.Wiki.Parser.Enum;
using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Quest;

public sealed class QuestDto : BaseDto
{
    public QuestDto()
    {
        ObjectType = ObjectTypeEnum.Quest;
    }
    
    [JsonIgnore]
    public int? Id { get; set; }
    public string Type { get; set; }          // Story, Archon, World, Hangout...
    public bool ShouldSerializeType() => !string.IsNullOrEmpty(Type);
    public string Chapter { get; set; }       // ex: "Historia Antiqua Chapter"
    public bool ShouldSerializeChapter() => !string.IsNullOrEmpty(Chapter);
    public int? ActNum { get; set; }          // 2
    public string Act { get; set; }           // "No Mere Stone"
    public int? Part { get; set; }            // 1
    public string Character { get; set; }     // "Zhongli"

    // Localização
    public string StartLocation { get; set; }
    public bool ShouldSerializeStartLocation() => !string.IsNullOrEmpty(StartLocation);
    public string Region { get; set; }
    public bool ShouldSerializeRegion() => !string.IsNullOrEmpty(Region);
    public string Area { get; set; }
    public bool ShouldSerializeArea() => !string.IsNullOrEmpty(Area);
    public string Subarea { get; set; }
    public bool ShouldSerializeSubarea()
    {
        if (string.IsNullOrEmpty(Subarea))
            return false;
        return !Subarea.Contains("|chapter", StringComparison.OrdinalIgnoreCase);
    }

    // Elenco
    public List<string> Characters { get; set; } = new();
    public bool ShouldSerializeCharacters() => Characters.Count > 0;

    // Conteúdo
    public string Description { get; set; }        // {{Quest Description|...}}

    // Diálogo (estrutura leve)
    public List<DialogueSection> Dialogues { get; set; } = new();
    public bool ShouldSerializeDialogues() => Dialogues.Count > 0;
}