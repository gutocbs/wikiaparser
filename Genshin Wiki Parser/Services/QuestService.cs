using Genshin.Wiki.Parser.Models.Quest;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers.Quest;

namespace Genshin.Wiki.Parser.Services;

public class QuestService
{
    public bool Set(Page page, string wikiText, string key)
    {
        QuestDto? quest = QuestParser.TryParse(wikiText, page.title);
        if (quest is not null)
        {
            page.About = quest;
            return true;
        }
        return false;
    }
}