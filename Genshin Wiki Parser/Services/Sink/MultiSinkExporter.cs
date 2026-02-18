using System.Text.Json;
using Genshin.Wiki.Parser.Models.Parse;
using Genshin.Wiki.Parser.Models.XML;

namespace Genshin.Wiki.Parser.Services.Sink;

public static class MultiSinkExporter
{
    public static void ExportPerType(
        IEnumerable<Page?> pages,
        IEnumerable<ParserRegistration> parsers,
        string outputDir,
        Func<Page, bool>? pagePredicate = null, // filtro extra (ex.: ignore list),
        string fileExtension = "txt"
    )
    {
        IEnumerable<ParserRegistration> parserRegistrations = parsers as ParserRegistration[] ?? parsers.ToArray();
        Dictionary<string, SharedArraySink<object>> sinks = new Dictionary<string, SharedArraySink<object>>();

        foreach (ParserRegistration parser in parserRegistrations)
        {
            sinks[parser.Key] = new SharedArraySink<object>(
                outRoot: Path.Combine(outputDir),
                fileExtension: fileExtension,
                filePrefix: parser.Key,
                maxBytesPerFile: 200L * 1024 * 1024,
                maxWordsPerFile: 500_000,
                maxFiles: 50,
                oversizeBehavior: SharedArraySink<object>.OversizeItemBehavior.AllowSingleFile,
                jsonOptions: new JsonSerializerOptions { WriteIndented = false }
            );
        }

        try
        {

            foreach (var page in pages)
            {
                if (page == null) continue;
                if (pagePredicate != null && !pagePredicate(page)) continue;

                foreach (var p in parsers)
                {
                    if (page.About == null) continue;

                    if (page.About.ObjectType == p.ObjectType && !string.IsNullOrWhiteSpace(page.title))
                    {
                        sinks[p.Key].Write(page.About);
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