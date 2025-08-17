using System.Xml;

namespace Genshin.Wiki.Parser;

class Program
{
    static void Main()
    {
        string xml = File.ReadAllText("F:\\Genshin Wiki Parser\\Genshin Wiki Parser\\genshin wiki.txt");

        // Carrega o XML
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);

        MediaWikiFilter.Process(
            doc,
            ignoreListPath: "IgnoreList.json",
            outputPath: "arquivos"
        );
        
        // // Converte para JSON formatado
        // using var sw = new StringWriter();
        // using var xw = new JsonTextWriter(sw);
        // JsonSerializer.Create().Serialize(xw, doc);
        // Root? jsonObject = JsonConvert.DeserializeObject<Root>(sw.ToString());
        //
        // if (jsonObject?.mediawiki.page != null)
        // {
        //     jsonObject.mediawiki.page =
        //         jsonObject.mediawiki.page
        //             .Where(p =>  !p.IgnoreTitles.Contains(p.title))
        //             .ToList();
        // }
        //
        // File.WriteAllText("saida.json", JsonConvert.SerializeObject(jsonObject, Newtonsoft.Json.Formatting.Indented));

        Console.WriteLine("Conversão concluída! Arquivo salvo como saida.json");
    }
}