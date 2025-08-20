using Genshin.Wiki.Parser.Enum;
using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models;

public class BaseDto
{
    [JsonIgnore]
    public ObjectTypeEnum ObjectType { get; set; }
    public string? Title { get; set; }

    public bool ShouldSerializeTitle() => !string.IsNullOrWhiteSpace(Title);
} 