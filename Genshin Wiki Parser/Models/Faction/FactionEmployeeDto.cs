namespace Genshin.Wiki.Parser.Models.Faction;

public sealed class FactionEmployeeDto
{
    public string? Name { get; set; }
    public string? Role { get; set; }           // “— Boss”, “— Chef” etc.
}