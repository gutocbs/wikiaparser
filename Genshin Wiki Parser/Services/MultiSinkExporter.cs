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
        var serializer = new JsonSerializer();
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

                var text = page.revision?.text?.content;
                if (string.IsNullOrWhiteSpace(text)) continue;

                foreach (var p in parsers)
                {
                    var dto = p.Parse(text);
                    if (dto != null)
                    {
                        sinks[p.Key].Write(serializer, dto);
                        if (p.Exclusive) break; // não tenta os outros parsers
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