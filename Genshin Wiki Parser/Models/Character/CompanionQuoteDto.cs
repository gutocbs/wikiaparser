namespace Genshin.Wiki.Parser.Models.Character;

public sealed class CompanionQuoteDto
{
    public string? Text { get; set; }
    public string? Context { get; set; } // ex.: "When the player is nearby"
}