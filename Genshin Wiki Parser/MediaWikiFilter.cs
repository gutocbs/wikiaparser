using System.Xml;
using Genshin.Wiki.Parser.Enum;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Parse;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Services;
using Genshin.Wiki.Parser.Services.Sink;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace Genshin.Wiki.Parser;

public static class MediaWikiFilter
{
    public static void Process(
        XmlDocument doc,
        string ignoreListPath,
        string outputPath)
    {
        // 1) Carrega ignore list
        (HashSet<string> ignoreTitles, List<string> ignoreKeywords) = IgnoreListHelper.Load(ignoreListPath);
        
        bool ShouldParse(Page page)
        {
            return !IgnoreListHelper.ShouldIgnore(page.title, ignoreTitles, ignoreKeywords) && 
                   !IgnoreListHelper.ShouldIgnore(page.revision.text.content, ignoreKeywords);
        }
        
        bool PagePredicate(Page page)
        {
            if (IgnoreListHelper.ShouldIgnore(page.title, ignoreTitles, ignoreKeywords))
                return false;
            if (IgnoreListHelper.ShouldIgnore(page.revision.text.content, ignoreKeywords))
                return false;
            return page.About is not null;
        }
        
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

        // 2) Converte XML -> JSON direto para um arquivo temp (sem string gigante)
        string tempJsonPath = Path.GetTempFileName();
        using (FileStream fs = new FileStream(tempJsonPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (StreamWriter sw = new StreamWriter(fs))
        using (JsonTextWriter jw = new JsonTextWriter(sw))
        {
            jw.Formatting = Formatting.None;
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(jw, doc); // <- escreve JSON direto no arquivo
        }

        // 3) Desserializa do temp para seu objeto Root (sem manter string)
        Root? root;
        using (FileStream fs = new FileStream(tempJsonPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (StreamReader sr = new StreamReader(fs))
        using (JsonTextReader jr = new JsonTextReader(sr))
        {
            JsonSerializer serializer = new JsonSerializer();
            root = serializer.Deserialize<Root>(jr);
        }

        // 4) Aplica o filtro (remove páginas por título)
        if (root?.mediawiki.pages != null)
        {
            root.mediawiki.pages = root.mediawiki.pages
                .Where(p => !IgnoreListHelper.ShouldIgnore(p.title, ignoreTitles, ignoreKeywords))
                .ToList();
            root.mediawiki.pages = root.mediawiki.pages
                .Where(p => !IgnoreListHelper.ShouldIgnore(p.revision.text.content, ignoreKeywords))
                .ToList();
        }

        if (root?.mediawiki.pages != null)
        {
            PlayableCharacterService playableCharacterService = new PlayableCharacterService();
            WeaponService weaponService = new WeaponService();
            ArtifactService artifactService = new ArtifactService();
            NpcService npcService = new NpcService();
            EnemyService enemyService = new EnemyService();
            FactionService factionService = new FactionService();
            BookService bookService = new BookService();
            LocationService locationService = new LocationService();
            ItemService itemService = new ItemService();
            FurnishingService furnishingService = new FurnishingService();
            QuestService questService = new QuestService();
            foreach (Page page in root.mediawiki.pages)
            {
                string wikiText = page.revision.text.content;
                if (string.IsNullOrWhiteSpace(wikiText)) continue;

                if (!ShouldParse(page)) continue;
                
                string key = TextHelper.GetBaseKey(page.title);
                if (string.IsNullOrEmpty(key)) continue;

                var parsed = playableCharacterService.Set(page, wikiText, key);
                //If was able to get the object, skip to next page
                if(parsed) continue;
                
                parsed = weaponService.Set(page, wikiText, key);
                if(parsed) continue;
                
                parsed = artifactService.Set(page, wikiText, key);
                if(parsed) continue;
                
                parsed = npcService.Set(page, wikiText, key);
                if(parsed) continue;
                
                parsed = enemyService.Set(page, wikiText, key);
                if(parsed) continue;
                
                parsed = factionService.Set(page, wikiText, key);
                if(parsed) continue;
                
                parsed = bookService.Set(page, wikiText, key);
                if(parsed) continue;
                
                parsed = locationService.Set(page, wikiText, key);
                if(parsed) continue;
                
                parsed = itemService.Set(page, wikiText, key);
                if(parsed) continue;
                
                parsed = furnishingService.Set(page, wikiText, key);
                if(parsed) continue;
                
                questService.Set(page, wikiText, key);
            }
        }
        
        //I'm exporting as txt to use with NotebookLM, as it does not accept json files
        MultiSinkExporter.ExportPerType(
            pages: root!.mediawiki.pages,
            parsers: parsers,
            outputDir: outputPath,
            pagePredicate: PagePredicate,
            fileExtension: "txt"
        );

        // 6) Limpeza do temp
        try { File.Delete(tempJsonPath); } catch { /* noop */ }
    }
}
