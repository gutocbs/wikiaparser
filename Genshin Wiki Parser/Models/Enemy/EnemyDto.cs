using Genshin.Wiki.Parser.Enum;

namespace Genshin.Wiki.Parser.Models.Enemy;

public sealed class EnemyDto : BaseDto
{
    public EnemyDto()
    {
        ObjectType = ObjectTypeEnum.Enemy;
    }
    
    // Infobox
    public string? Name { get; set; }                 // Elite Enemies, Common, Boss...
    public string? Type { get; set; }                 // Elite Enemies, Common, Boss...
    public List<string>? DamageTypes { get; set; }    // Physical, Geo...
    public string? Family { get; set; }               // Hilichurls
    public string? Group { get; set; }                // Mitachurls

    // Templates auxiliares
    public string? ShortDescription { get; set; }     // {{Description|...}} (primeiro)
    public string? DescriptionsSection { get; set; }  // seção ==Descriptions== limpa
}

