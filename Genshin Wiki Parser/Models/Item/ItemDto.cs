using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Item;

public sealed class ItemDto : BaseDto
{
    public ItemDto()
    {
        ObjectType = Enum.ObjectTypeEnum.Item;
    }
    
    [JsonIgnore]
    public int? Id { get; set; }
    public string Type { get; set; }        // ex: Quest Items
    public string Group { get; set; }       // opcional
    public int? Quality { get; set; }       // 1..5 se existir no infobox
    public string Description { get; set; }
    public List<string> Sources { get; set; } = new();
}
