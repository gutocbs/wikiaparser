using System.Xml;
using Genshin.Wiki.Parser.Enum;
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
        string outputPath)
    {
        (HashSet<string> ignoreTitles, List<string> ignoreKeywords) = IgnoreListHelper.Load(ignoreListPath);

        List<ParserRegistration> parsers =
        [
            new("playableCharacters", ObjectTypeEnum.PlayableCharacter, true, ShardMode.Count, 50),
            new("npcs", ObjectTypeEnum.NonPlayableCharacter, true, ShardMode.Count, 250),
            new("quest", ObjectTypeEnum.Quest, true, ShardMode.Count, 100),
            new("weapons", ObjectTypeEnum.Weapon, true),
            new("artifacts", ObjectTypeEnum.Artifact, true),
            new("enemy", ObjectTypeEnum.Enemy, true),
            new("factions", ObjectTypeEnum.Faction, true),
            new("books", ObjectTypeEnum.Book, true),
            new("location", ObjectTypeEnum.Location, true),
            new("item", ObjectTypeEnum.Item, true),
            new("furnishing", ObjectTypeEnum.Furnishing, true)
        ];

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

        var parsedPages = new List<Page>();

        foreach (Page page in ReadPages(inputPath, ignoreTitles, ignoreKeywords))
        {
            string wikiText = page.revision.text.content;
            if (string.IsNullOrWhiteSpace(wikiText)) continue;

            if (IgnoreListHelper.ShouldIgnore(wikiText, ignoreKeywords)) continue;

            string key = TextHelper.GetBaseKey(page.title);
            if (string.IsNullOrEmpty(key)) continue;

            var parsed = playableCharacterService.Set(page, wikiText, key);
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

            // Keep parsed DTOs for final export, but release the raw wikitext immediately.
            page.revision.text.content = string.Empty;
            parsedPages.Add(page);
        }

        // Export after all pages were visited so services can attach late companion data
        // such as Lore, Voice-Overs and Namecards to already parsed character DTOs.
        MultiSinkExporter.ExportPerType(
            pages: parsedPages,
            parsers: parsers,
            outputDir: outputPath,
            pagePredicate: static page => page.About is not null,
            fileExtension: "txt"
        );
    }

    private static IEnumerable<Page> ReadPages(
        string inputPath,
        HashSet<string> ignoreTitles,
        List<string> ignoreKeywords)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true
        };

        using var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1024 * 128);
        using var reader = XmlReader.Create(stream, settings);

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "page") continue;

            using var pageReader = reader.ReadSubtree();
            Page? page = ReadPage(pageReader, ignoreTitles, ignoreKeywords);
            if (page is not null) yield return page;
        }
    }

    private static Page? ReadPage(
        XmlReader reader,
        HashSet<string> ignoreTitles,
        List<string> ignoreKeywords)
    {
        var page = new Page
        {
            title = string.Empty,
            id = null,
            revision = new Revision
            {
                text = new Text { content = string.Empty }
            }
        };

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element) continue;

            switch (reader.LocalName)
            {
                case "title":
                    page.title = reader.ReadElementContentAsString();
                    if (IgnoreListHelper.ShouldIgnore(page.title, ignoreTitles, ignoreKeywords))
                    {
                        return null;
                    }

                    break;
                case "id" when page.id is null:
                    page.id = reader.ReadElementContentAsString();
                    break;
                case "revision":
                    using (var revisionReader = reader.ReadSubtree())
                    {
                        page.revision = ReadRevision(revisionReader);
                    }
                    break;
            }
        }

        return string.IsNullOrWhiteSpace(page.title) ? null : page;
    }

    private static Revision ReadRevision(XmlReader reader)
    {
        var revision = new Revision
        {
            text = new Text { content = string.Empty }
        };

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "text")
            {
                revision.text.content = reader.ReadElementContentAsString();
                break;
            }
        }

        return revision;
    }
}
