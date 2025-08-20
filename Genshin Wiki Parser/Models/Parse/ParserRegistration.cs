using Genshin.Wiki.Parser.Enum;

namespace Genshin.Wiki.Parser.Models.Parse;

public sealed class ParserRegistration
{
    public string Key { get; }                      // nome do arquivo (ex.: "characters")
    public bool Exclusive { get; }                  // se true, para no primeiro que casar

    public ObjectTypeEnum ObjectType { get; }       // tipo do objeto que o parser retorna
    
    public ParserRegistration(string key, ObjectTypeEnum objectType, bool exclusive = false)
    {
        Key = key;
        ObjectType = objectType;
        Exclusive = exclusive;
    }
}