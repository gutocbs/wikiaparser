using Genshin.Wiki.Parser.Settings;

namespace Genshin.Wiki.Parser;

class Program
{
    static void Main(string[] args)
    {
        RuntimeSettings runtimeSettings = JsonConfigReader.Load<RuntimeSettings>("runtime-settings.json");
        ParserRegistrationSettings parserRegistrationSettings = JsonConfigReader.Load<ParserRegistrationSettings>("parser-registrations.json");
        ExportSettings exportSettings = JsonConfigReader.Load<ExportSettings>("export-settings.json");

        string inputFile = runtimeSettings.Defaults.InputFile;
        string outputPath = runtimeSettings.Defaults.OutputPath;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--input" && i + 1 < args.Length)
                inputFile = args[i + 1];
            if (args[i] == "--out" && i + 1 < args.Length)
                outputPath = args[i + 1];
        }

        string path = ResolveFromBaseDirectory(inputFile);

        Console.WriteLine("Reading file " + path);

        MediaWikiFilter.Process(
            path,
            ignoreListPath: ResolveFromBaseDirectory(runtimeSettings.Defaults.IgnoreListPath),
            outputPath: outputPath,
            parsers: parserRegistrationSettings.ToParserRegistrations(),
            exportSettings: exportSettings
        );

        Console.WriteLine("File parserd successfully!");
    }

    private static string ResolveFromBaseDirectory(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        return Path.Combine(AppContext.BaseDirectory, path);
    }
}
