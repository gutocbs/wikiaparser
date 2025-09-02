using Genshin.Wiki.Parser.Enum;
using Genshin.Wiki.Parser.Models.Parse;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Services.Interfaces;
using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Services;

public static class MultiSinkExporter
{
    public static void ExportPerType(
        IEnumerable<Page?> pages,
        IEnumerable<ParserRegistration> parsers,
        string outputDir,
        Func<Page, bool>? pagePredicate = null, // filtro extra (ex.: ignore list),
        string fileExtension = "json"
    )
    {
        var serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        
        IEnumerable<ParserRegistration> parserRegistrations = parsers as ParserRegistration[] ?? parsers.ToArray();
        Dictionary<string, IObjectSink> sinks = parserRegistrations.ToDictionary<ParserRegistration, string, IObjectSink>(
            p => p.Key,
            p => p.ShouldShard ? 
                new ShardedArraySink(
                    outputDir: Path.Combine(outputDir, $"{p.Key}"),
                    baseName: $"{p.Key}",
                    shardMode: p.ShardMode,
                    maxPerFile: p.MaxShardCount,
                    fileExtension: fileExtension
                ) : 
                new OutputSink(Path.Combine(outputDir, $"{p.Key}.{fileExtension}"))
        );

        try
        {
            foreach (var page in pages)
            {
                if (page == null) continue;
                if (pagePredicate != null && !pagePredicate(page)) continue;

                foreach (var p in parserRegistrations)
                {
                    if (page.About != null)
                    {
                        if (page.About.ObjectType == p.ObjectType && !string.IsNullOrWhiteSpace(page.title)) 
                            sinks[p.Key].Write(serializer, page.About);
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