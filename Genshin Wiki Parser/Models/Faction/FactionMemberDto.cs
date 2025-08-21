namespace Genshin.Wiki.Parser.Models.Faction;

public sealed class FactionMemberDto
{
    public string? Name { get; set; }           // display
    public string? NameLink { get; set; }       // alvo do link (se houver)
    public string? Title { get; set; }          // Tianquan, Yuheng...
    public string? Star { get; set; }           // Dubhe / (Alpha Ursae Majoris)
    public string? Responsibility { get; set; } // Laws, Urban Management...
}