using Genshin.Wiki.Parser.Models.Furnishing;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers.Furnishing;

namespace Genshin.Wiki.Parser.Services;

public class FurnishingService
{
    public bool Set(Page page, string wikiText, string key)
    {
        FurnishingDto? furnishingDto = FurnishingParser.TryParse(wikiText, page.title);
        if (furnishingDto is not null)
        {
            page.About = furnishingDto;
            return true;
        }
        return false;
    }
}