using System.Diagnostics;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Parse;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Services;
using Genshin.Wiki.Parser.Services.Sink;
using Genshin.Wiki.Parser.Settings;

namespace Genshin.Wiki.Parser;

public static class MediaWikiFilter
{
    public static void Process(
        string inputPath,
        string ignoreListPath,
        string outputPath,
        IEnumerable<ParserRegistration> parsers,
        ExportSettings exportSettings,
        int readerQueueBatches = 0,
        int readerBatchSize = 64)
    {
        (HashSet<string> ignoreTitles, List<string> ignoreKeywords) = IgnoreListHelper.Load(ignoreListPath);
        ParserRegistration[] parserRegistrations = parsers as ParserRegistration[] ?? parsers.ToArray();

        PlayableCharacterService playableCharacterService = new();
        WeaponService weaponService = new();
        ArtifactService artifactService = new();
        NpcService npcService = new();
        EnemyService enemyService = new();
        FactionService factionService = new();
        BookService bookService = new();
        LocationService locationService = new();
        ItemService itemService = new();
        FurnishingService furnishingService = new();
        QuestService questService = new();

        List<Page> parsedPages = new List<Page>();

        void ProcessPage(Page page)
        {
            string wikiText = page.revision.text.content;
            if (string.IsNullOrWhiteSpace(wikiText)) return;

            if (IgnoreListHelper.ShouldIgnore(wikiText, ignoreKeywords)) return;

            string key = TextHelper.GetBaseKey(page.title);
            if (string.IsNullOrEmpty(key)) return;

            bool parsed = playableCharacterService.Set(page, wikiText, key);
            if (!parsed) parsed = weaponService.Set(page, wikiText, key);
            if (!parsed) parsed = artifactService.Set(page, wikiText, key);
            if (!parsed) parsed = npcService.Set(page, wikiText, key);
            if (!parsed) parsed = enemyService.Set(page, wikiText, key);
            if (!parsed) parsed = factionService.Set(page, wikiText, key);
            if (!parsed) parsed = bookService.Set(page, wikiText, key);
            if (!parsed) parsed = locationService.Set(page, wikiText, key);
            if (!parsed) parsed = itemService.Set(page, wikiText, key);
            if (!parsed) parsed = furnishingService.Set(page, wikiText, key);
            if (!parsed) questService.Set(page, wikiText, key);

            if (page.About is null) return;

            // Keep parsed DTOs for final export but release the raw wikitext immediately.
            page.revision.text.content = string.Empty;
            parsedPages.Add(page);
        }

        IEnumerable<Page> pages = MediaWikiPageReader.ReadPages(
            inputPath,
            title => IgnoreListHelper.ShouldIgnore(title, ignoreTitles, ignoreKeywords));

        if (readerQueueBatches > 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(readerBatchSize);

            // Pipeline: uma thread lê o XML enquanto esta thread faz o parse, na mesma ordem.
            // Uma única thread de parse basta: o parse é mais rápido que a leitura (sequencial por natureza),
            // então threads extras de parse só ficariam esperando páginas.
            // As páginas passam em lotes: uma sincronização por lote em vez de uma por página (~460 mil).
            long parseTicks = 0;
            int consumed = 0;
            PipelineStats stats = ReadAheadPipeline.Run(pages.Chunk(readerBatchSize), batch =>
            {
                long t0 = Stopwatch.GetTimestamp();
                foreach (Page page in batch) ProcessPage(page);
                parseTicks += Stopwatch.GetTimestamp() - t0;
                if ((++consumed & 63) == 0) RunMetrics.SampleThreads();
            }, readerQueueBatches);

            RunMetrics.AddTime("parse", parseTicks);
            RunMetrics.AddTime("parseWaitingForReader", stats.ConsumerWaitTicks);
            RunMetrics.AddTime("readerWaitingForParse", stats.ProducerBlockedTicks);
        }
        else
        {
            // Sequencial: mede separadamente o tempo dentro do leitor ("read") e no filtro + parse ("parse").
            long readTicks = 0, parseTicks = 0;
            using IEnumerator<Page> enumerator = pages.GetEnumerator();
            while (true)
            {
                long t0 = Stopwatch.GetTimestamp();
                bool hasPage = enumerator.MoveNext();
                long t1 = Stopwatch.GetTimestamp();
                readTicks += t1 - t0;
                if (!hasPage) break;

                ProcessPage(enumerator.Current);
                parseTicks += Stopwatch.GetTimestamp() - t1;
            }

            RunMetrics.AddTime("read", readTicks);
            RunMetrics.AddTime("parse", parseTicks);
        }

        RunMetrics.EndPhase("readAndParse");

        // Export after all pages were visited so services can attach late companion data
        // such as Lore, Voice-Overs and Namecards to already parsed character DTOs.
        MultiSinkExporter.ExportPerType(
            pages: parsedPages,
            parsers: parserRegistrations,
            outputDir: outputPath,
            exportSettings: exportSettings,
            pagePredicate: static page => page.About is not null,
            fileExtension: exportSettings.DefaultFileExtension
        );
        RunMetrics.EndPhase("export");
    }

}
