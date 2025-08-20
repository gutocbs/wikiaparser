using Genshin.Wiki.Parser.Models.Parse;
using Genshin.Wiki.Parser.Models.XML;
using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Services;

public static class MultiSinkExporter
{
    public static void ExportPerType(
        IEnumerable<Page> pages,
        IEnumerable<ParserRegistration> parsers,
        string outputDir,
        Func<Page, bool>? pagePredicate = null // filtro extra (ex.: ignore list)
    )
    {
        var serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        // cria um sink por parser
        var sinks = parsers.ToDictionary(
            p => p.Key,
            p => new OutputSink(Path.Combine(outputDir, $"{p.Key}.json"))
        );

        try
        {
            foreach (var page in pages)
            {
                if (page == null) continue;
                if (pagePredicate != null && !pagePredicate(page)) continue;

                foreach (var p in parsers)
                {
                    if (page.About != null)
                    {
                        if (page.About.ObjectType == p.ObjectType && !string.IsNullOrWhiteSpace(page.title))
                        {
                            sinks[p.Key].Write(serializer, page.About);
                            if (p.Exclusive) break; // não tenta os outros parsers
                        }
                    }
                }
            }
        }
        finally
        {
            // fecha todos os arquivos mesmo se der erro
            foreach (var s in sinks.Values) s.Dispose();
        }
    }
}