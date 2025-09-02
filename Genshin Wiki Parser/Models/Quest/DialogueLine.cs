namespace Genshin.Wiki.Parser.Models.Quest;

public sealed class DialogueLine
{
    public string? Speaker { get; set; }     // "Paimon", "Zhongli", "[Choice]"
    public string Text { get; set; }
}