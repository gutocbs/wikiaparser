using Genshin.Wiki.Parser.Models.Faction;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers.Faction;

namespace Genshin.Wiki.Parser.Services;

public class FactionService
{
    public bool Set(Page page, string wikiText, string key)
    {
        FactionDto? factionDto = FactionParser.TryParse(wikiText, page.title);
        if (factionDto is not null)
        {
            page.About = factionDto;
            return true;
        }

        return false;
    }
}