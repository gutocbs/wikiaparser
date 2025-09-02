namespace Genshin.Wiki.Parser.Models.Quest;

public sealed class DialogueLine
{
    public string? Speaker { get; set; }     // "Paimon", "Zhongli", "[Choice]"
    public bool ShouldSerializeSpeaker() => !string.IsNullOrEmpty(Speaker);
    public string Text { get; set; }
}