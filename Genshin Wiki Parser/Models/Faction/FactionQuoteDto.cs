namespace Genshin.Wiki.Parser.Models.Faction;

public sealed class FactionQuoteDto
{
    public string? Text { get; set; }
    public string? Speaker { get; set; }    // Keqing, etc.
    public string? Context { get; set; }    // during The Floating Palace...
}