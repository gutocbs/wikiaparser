namespace Genshin.Wiki.Parser.Models.Quest;

public sealed class DialogueSection
{
    public string Context { get; set; } // ex: "Go to the Liyue Adventurers' Guild and talk to Katheryne"
    public List<DialogueLine> Lines { get; set; } = new();
}