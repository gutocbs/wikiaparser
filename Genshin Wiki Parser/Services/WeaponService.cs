using Genshin.Wiki.Parser.Models.Weapon;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers.Weapon;

namespace Genshin.Wiki.Parser.Services;

public class WeaponService
{
    public bool Set(Page page, string wikiText, string key)
    {
        WeaponDto? weapon = WeaponParser.TryParse(wikiText);
        if (weapon is not null)
        {
            page.About = weapon;
            return true;
        }
        return false;
    }
}