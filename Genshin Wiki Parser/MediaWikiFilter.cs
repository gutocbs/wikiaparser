using System.Xml;
using Genshin.Wiki.Parser.Enum;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Parse;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Services;
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
        // 1) Carrega ignore list (leve)
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
        
        List<ParserRegistration> parsers = new List<ParserRegistration>
        {
            new("playableCharacters", ObjectTypeEnum.PlayableCharacter),
            new("weapons",    ObjectTypeEnum.Weapon),
            new("artifacts",    ObjectTypeEnum.Artifact),
            new("npcs",    ObjectTypeEnum.NonPlayableCharacter),
            new("enemy",    ObjectTypeEnum.Enemy),
            new("factions",    ObjectTypeEnum.Faction),
            new("books",    ObjectTypeEnum.Book),
            new("location",    ObjectTypeEnum.Location),
            new("item",    ObjectTypeEnum.Item),
            new("furnishing",    ObjectTypeEnum.Furnishing),
            new("quest",    ObjectTypeEnum.Quest),
        };

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
                if(parsed)
                    continue;
                // 2) Tenta parsear Armas
                parsed = weaponService.Set(page, wikiText, key);
                if(parsed)
                    continue;
                // 3) Tenta parsear Artefatos
                parsed = artifactService.Set(page, wikiText, key);
                if(parsed)
                    continue;
                // 4) Tenta parsear NPCs
                parsed = npcService.Set(page, wikiText, key);
                if(parsed)
                    continue;
                // 5) Tenta parsear Enemies
                parsed = enemyService.Set(page, wikiText, key);
                if(parsed)
                    continue;
                // 6) Tenta parsear Factions
                parsed = factionService.Set(page, wikiText, key);
                if(parsed)
                    continue;
                // 7) Tenta parsear Livros
                parsed = bookService.Set(page, wikiText, key);
                if(parsed)
                    continue;
                // 8) Tenta parsear Livros
                parsed = locationService.Set(page, wikiText, key);
                if(parsed)
                    continue;
                // 9) Tenta parsear itens
                parsed = itemService.Set(page, wikiText, key);
                if(parsed)
                    continue;
                // 10) Tenta parsear moveis
                parsed = furnishingService.Set(page, wikiText, key);
                if(parsed)
                    continue;
                // 11) Tenta parsear quests
                parsed = questService.Set(page, wikiText, key);
                if(parsed)
                    continue;
            }
        }
        
        MultiSinkExporter.ExportPerType(
            pages: root!.mediawiki.pages,
            parsers: parsers,
            outputDir: outputPath,
            pagePredicate: PagePredicate
        );

        // 6) Limpeza do temp
        try { File.Delete(tempJsonPath); } catch { /* noop */ }
    }
}
