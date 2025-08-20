namespace Genshin.Wiki.Parser.Models.XML;

public class Page
{
    public string title { get; set; }
    public BaseDto? About { get; set; }
    public string? id { get; set; }
    public Revision revision { get; set; }
}