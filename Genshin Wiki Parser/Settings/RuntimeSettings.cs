namespace Genshin.Wiki.Parser.Settings;

public sealed class RuntimeSettings
{
    public RuntimeDefaults Defaults { get; set; } = new();
}

public sealed class RuntimeDefaults
{
    public string InputFile { get; set; } = "gensinimpact_pages_current.xml";
    public string OutputPath { get; set; } = "files";
    public string IgnoreListPath { get; set; } = "IgnoreList.json";
}
