namespace Genshin.Wiki.Parser.Models.Weapon;

public sealed class WeaponAscensionDto
{
    public List<string>? AscendMats { get; set; }      // ascendMat1..4
    public List<string>? BossMats { get; set; }        // bossMat1..3
    public List<string>? CommonMats { get; set; }      // commonMat1..3
}