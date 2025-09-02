using System.Xml;

namespace Genshin.Wiki.Parser;

class Program
{
    static void Main()
    {
        string path = Path.GetDirectoryName(AppContext.BaseDirectory) ?? string.Empty;
        path = Path.Combine(path, "gensinimpact_pages_current.xml");
        string xml = File.ReadAllText(path);

        // Carrega o XML
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);

        MediaWikiFilter.Process(
            doc,
            ignoreListPath: "IgnoreList.json",
            outputPath: "files"
        );

        Console.WriteLine("Conversão concluída! Arquivo salvo como saida.json");
    }
}