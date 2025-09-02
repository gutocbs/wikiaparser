using Genshin.Wiki.Parser.Models.Books;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers.Book;

namespace Genshin.Wiki.Parser.Services;

public class BookService
{
    public bool Set(Page page, string wikiText, string key)
    {
        BookCollectionDto? bookCollection = BookCollectionParser.TryParse(wikiText, page.title);
        if (bookCollection is not null)
        {
            page.About = bookCollection;
            return true;
        }
        return false;
    }
}