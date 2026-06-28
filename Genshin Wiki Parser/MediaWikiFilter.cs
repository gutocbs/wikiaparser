using Genshin.Wiki.Parser.Configuration;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Parse;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Services;
using Genshin.Wiki.Parser.Services.Sink;

namespace Genshin.Wiki.Parser;

public static class MediaWikiFilter
{
    public static void Process(
        string inputPath,
        string ignoreListPath,
        string outputPath,
        IEnumerable<ParserRegistration> parsers,
        ExportSettings exportSettings)
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

        foreach (Page page in MediaWikiPageReader.ReadPages(
                     inputPath,
                     title => IgnoreListHelper.ShouldIgnore(title, ignoreTitles, ignoreKeywords)))
        {
            string wikiText = page.revision.text.content;
            if (string.IsNullOrWhiteSpace(wikiText)) continue;

            if (IgnoreListHelper.ShouldIgnore(wikiText, ignoreKeywords)) continue;

            string key = TextHelper.GetBaseKey(page.title);
            if (string.IsNullOrEmpty(key)) continue;

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

            if (page.About is null) continue;

            // Keep parsed DTOs for final export but release the raw wikitext immediately.
            page.revision.text.content = string.Empty;
            parsedPages.Add(page);
        }

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
    }

}
