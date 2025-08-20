using Genshin.Wiki.Parser.Models.Enemy;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers.Enemy;

namespace Genshin.Wiki.Parser.Services;

public class EnemyService
{
    public bool Set(Page page, string wikiText, string key)
    {
        EnemyDto? enemyDto = EnemyParser.TryParse(wikiText, page.title);
        if (enemyDto is not null)
        {
            page.About = enemyDto;
            return true;
        }

        return false;
    }
}