using Genshin.Wiki.Parser.Models.Character;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers.Character;

namespace Genshin.Wiki.Parser.Services;

public class NpcService
{
    public bool Set(Page page, string wikiText, string key)
    {
        if (!wikiText.Contains("NPC", StringComparison.OrdinalIgnoreCase))
            return false;

        NpcDto? artifactPiece = NpcParser.TryParse(wikiText, page.title);
        if (artifactPiece is not null)
        {
            page.About = artifactPiece;
            return true;
        }
        return false;
    }
}