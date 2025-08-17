namespace Genshin.Wiki.Parser.Models.Parse;

public sealed class ParserRegistration
{
    public string Key { get; }                      // nome do arquivo (ex.: "characters")
    public Func<string, object?> Parse { get; }     // recebe wikitext e devolve DTO ou null
    public bool Exclusive { get; }                  // se true, para no primeiro que casar

    public ParserRegistration(string key, Func<string, object?> parse, bool exclusive = true)
    {
        Key = key;
        Parse = parse;
        Exclusive = exclusive;
    }
}