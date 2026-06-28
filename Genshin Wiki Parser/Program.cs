namespace Genshin.Wiki.Parser;

class Program
{
    static void Main(string[] args)
    {
        string inputFile = "gensinimpact_pages_current.xml";
        string outputPath = "files";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--input" && i + 1 < args.Length)
                inputFile = args[i + 1];
            if (args[i] == "--out" && i + 1 < args.Length)
                outputPath = args[i + 1];
        }

        string path = Path.GetDirectoryName(AppContext.BaseDirectory) ?? string.Empty;
        path = Path.Combine(path, inputFile);

        Console.WriteLine("Reading file " + path);

        MediaWikiFilter.Process(
            path,
            ignoreListPath: "IgnoreList.json",
            outputPath: outputPath
        );

        Console.WriteLine("File parserd successfully!");
    }
}
