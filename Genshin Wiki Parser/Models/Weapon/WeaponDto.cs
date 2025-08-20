using Genshin.Wiki.Parser.Enum;
using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Models.Weapon;

public sealed class WeaponDto : BaseDto
{
    public WeaponDto()
    {
        ObjectType = ObjectTypeEnum.Weapon;
    }
    
    [JsonIgnore]
    public int? Id { get; set; }                       // 11401
    public string? Type { get; set; }                  // Sword
    public string? Series { get; set; }                // Favonius
    public int? Quality { get; set; }                  // 4
    public int? BaseAtk { get; set; }                  // 41
    public string? SecondaryStatType { get; set; }     // Energy Recharge
    public string? SecondaryStat { get; set; }         // 13.3%
    public string? Obtain { get; set; }                // Wishes (texto livre)
    public string? PassiveName { get; set; }           // Windfall
    public string? PassiveEffectTemplate { get; set; } // "CRIT hits have a {var1}% ... every {var2}s."
    public Dictionary<string, string[]>? PassiveVars { get; set; } // "var1" -> [r1..r5], "var2" -> [...]
    public List<string>? PassiveAttributes { get; set; }           // ["CRIT Hit", "Energy Generation"]
    public WeaponAscensionDto? Ascension { get; set; }
    public string? ShortDescription { get; set; }      // do {{Description|...}}
    public string? LongDescription { get; set; }       // da seção ==Description== (opcional)
}