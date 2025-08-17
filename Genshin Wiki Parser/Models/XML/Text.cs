using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.XML;

public class Text
{
    [JsonProperty("#text")]
    public string content { get; set; }
    public bool ShouldSerializecontent() => false; // não serializa, mas desserializa normal
}