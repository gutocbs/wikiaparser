namespace Genshin.Wiki.Parser.Models.Character;

public sealed class NamecardImagesDto
{
    public string? Icon { get; set; }
    public string? Background { get; set; }
    public string? Banner { get; set; }
    public List<string>? Extras { get; set; } // linhas não mapeadas do <gallery>
}