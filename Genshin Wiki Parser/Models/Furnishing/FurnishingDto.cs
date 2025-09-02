using Genshin.Wiki.Parser.Enum;

namespace Genshin.Wiki.Parser.Models.Furnishing;

public sealed class FurnishingDto : BaseDto
{
    public FurnishingDto()
    {
        ObjectType = ObjectTypeEnum.Furnishing;
    }
    
    public string Category { get; set; }      // Large Furnishing, Ornament etc.
    public string Subcategory { get; set; }   // Bed, Table, Wall Ornament...

    public string Description { get; set; }

    // Fontes de obtenção (infobox: source1, source2, ...)
    public List<string> Sources { get; set; } = new();

    // Onde pega a *blueprint* (infobox: blueprint) e custos (corpo do texto)
    public List<string> BlueprintSources { get; set; } = new();
}