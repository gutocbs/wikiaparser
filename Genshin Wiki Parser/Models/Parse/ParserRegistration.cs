using Genshin.Wiki.Parser.Enum;

namespace Genshin.Wiki.Parser.Models.Parse;

public sealed class ParserRegistration
{
    public string Key { get; }                      // nome do arquivo (ex.: "characters")
    public bool ShouldShard { get; }                  // se true, quebra o arquivo em vários (ex.: characters_A.json, characters_B.json, ...)
    public int MaxShardCount { get; }                  // se true, quebra o arquivo em vários (ex.: characters_A.json, characters_B.json, ...)
    public ObjectTypeEnum ObjectType { get; }       // tipo do objeto que o parser retorna
    
    public ParserRegistration(string key, ObjectTypeEnum objectType, bool shouldShard = false, int maxShardCount = 0)
    {
        Key = key;
        ObjectType = objectType;
        ShouldShard = shouldShard;
        MaxShardCount = maxShardCount;
    }
}