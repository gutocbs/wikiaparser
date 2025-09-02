using Genshin.Wiki.Parser.Models.Location;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers.Location;

namespace Genshin.Wiki.Parser.Services;

public class LocationService
{
    public bool Set(Page page, string wikiText, string key)
    {
        LocationDto? locationDto = LocationParser.TryParse(wikiText, page.title);
        if (locationDto is not null)
        {
            page.About = locationDto;
            return true;
        }
        return false;
    }
}