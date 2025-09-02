namespace Genshin.Wiki.Parser.Models.Location;

public sealed class LocationDto : BaseDto
{
    public LocationDto()
    {
        ObjectType = Enum.ObjectTypeEnum.Location;
    }
    
    public string Type { get; set; }          // ex: Point of Interest
    public string Subtype { get; set; }       // ex: Blacksmith (type2)
    public string Region { get; set; }        // ex: Mondstadt
    public string Area { get; set; }          // ex: Starfell Valley
    public string Subarea { get; set; }       // ex: Mondstadt City
    public string Summary { get; set; }       // do Location Intro
    public bool ShouldSerializeSummary()
    {
        if (string.IsNullOrEmpty(Summary))
            return false;
        return !Summary.Contains("{{If Self|");
    }

    public List<string> Npcs { get; set; } = new();
    public bool ShouldSerializeNpcs() => Npcs.Count > 0;
    public List<LocationDescriptionDto> Descriptions { get; set; } = new();
    public bool ShouldSerializeDescriptions() => Descriptions.Count > 0;
}