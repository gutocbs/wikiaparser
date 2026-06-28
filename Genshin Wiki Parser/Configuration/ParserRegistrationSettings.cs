using Genshin.Wiki.Parser.Enum;
using Genshin.Wiki.Parser.Models.Parse;

namespace Genshin.Wiki.Parser.Configuration;

public sealed class ParserRegistrationSettings
{
    public List<ParserRegistrationConfig> Parsers { get; set; } = new();

    public List<ParserRegistration> ToParserRegistrations()
    {
        return Parsers
            .Select(static parser => parser.ToParserRegistration())
            .ToList();
    }
}

public sealed class ParserRegistrationConfig
{
    public string Key { get; set; } = string.Empty;
    public ObjectTypeEnum ObjectType { get; set; }
    public bool ShouldShard { get; set; }
    public ShardMode ShardMode { get; set; } = ShardMode.Count;
    public int MaxShardCount { get; set; }

    public ParserRegistration ToParserRegistration()
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new InvalidOperationException("Parser registration key is required.");
        }

        return new ParserRegistration(Key, ObjectType, ShouldShard, ShardMode, MaxShardCount);
    }
}
