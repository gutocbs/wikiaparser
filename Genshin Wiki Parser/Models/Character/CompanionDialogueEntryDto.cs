namespace Genshin.Wiki.Parser.Models.Character;

public sealed class CompanionDialogueEntryDto
{
    public string Role { get; set; } = "NPC";  // "NPC" ou "Player"
    public string? Text { get; set; }
    public List<string>? AudioFiles { get; set; } // nomes .ogg se existir {{A|...}}
    public string? ChoiceGroup { get; set; } // id do grupo de escolhas (para agrupar Player→respostas)
}