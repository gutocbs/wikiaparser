using System.Xml;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models;
using Genshin.Wiki.Parser.Models.Character;
using Genshin.Wiki.Parser.Models.Parse;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers;
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

        // 2) Converte XML -> JSON direto para um arquivo temp (sem string gigante)
        string tempJsonPath = Path.GetTempFileName();
        using (FileStream fs = new FileStream(tempJsonPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (StreamWriter sw = new StreamWriter(fs))
        using (JsonTextWriter jw = new JsonTextWriter(sw) { Formatting = Formatting.None })
        {
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
            foreach (Page page in root?.mediawiki.pages!)
            {
                string? wikiText = page.revision?.text.content;
        
                CharacterDto? dto = CharacterParser.TryParseCharacter(wikiText);
                if (page.revision != null && dto != null)
                    page.About = dto;
            }
        }
        
        var parsers = new List<ParserRegistration>
        {
            new("characters", CharacterParser.TryParseCharacter),
            // new("weapons",    text => WeaponPageParser.TryParseWeapon(text)),
            // new("locations",  text => LocationPageParser.TryParseLocation(text)),
            // ...
        };
        
        bool PagePredicate(Page page)
        {
            var title = page.title;
            if (IgnoreListHelper.ShouldIgnore(title, ignoreTitles, ignoreKeywords))
                return false;
            if (IgnoreListHelper.ShouldIgnore(page.revision.text.content, ignoreKeywords))
                return false;
            return page.About is not null;
        }
        
        MultiSinkExporter.ExportPerType(
            pages: root.mediawiki.pages,
            parsers: parsers,
            outputDir: outputPath,
            pagePredicate: PagePredicate
        );

        // 5) Salva o JSON final direto no disco (sem string em memória)
        using (FileStream fs = new FileStream($"{outputPath}/saida.txt",FileMode.Create, FileAccess.Write, FileShare.None))
        using (StreamWriter sw = new StreamWriter(fs))
        using (JsonTextWriter jw = new JsonTextWriter(sw) { Formatting = Formatting.Indented })
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(jw, root);
        }

        // 6) Limpeza do temp
        try { File.Delete(tempJsonPath); } catch { /* noop */ }
    }
    
    
}
