namespace Genshin.Wiki.Parser.Models.Weapon;

public sealed class WeaponImagesDto
{
    public string? Base { get; set; }                  // "Weapon Favonius Sword.png"
    public string? Ascension2 { get; set; }            // "Weapon Favonius Sword 2nd.png"
    public List<string>? Extras { get; set; }          // outras linhas do <gallery> do infobox
}