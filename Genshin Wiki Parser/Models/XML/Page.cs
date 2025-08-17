namespace Genshin.Wiki.Parser.Models.XML;

public class Page
{
    public string title { get; set; }
    public object? About { get; set; }
    public Revision revision { get; set; }
}