using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.XML;

public class Namespace
{
    [JsonProperty("@key")]
    public string key { get; set; }

    [JsonProperty("@case")]
    public string @case { get; set; }

    [JsonProperty("#text")]
    public string text { get; set; }
}