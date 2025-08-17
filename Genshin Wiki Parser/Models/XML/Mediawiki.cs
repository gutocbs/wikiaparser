using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.XML;

public class Mediawiki
{
    public Siteinfo siteinfo { get; set; }
    [JsonProperty("page")]
    public List<Page> pages { get; set; }
}