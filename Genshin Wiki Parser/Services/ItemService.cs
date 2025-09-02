using Genshin.Wiki.Parser.Models.Item;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers.Item;

namespace Genshin.Wiki.Parser.Services;

public class ItemService
{
    public bool Set(Page page, string wikiText, string key)
    {
        ItemDto? item = ItemParser.TryParse(wikiText, page.title);
        if (item is not null)
        {
            page.About = item;
            return true;
        }
        return false;
    }
}