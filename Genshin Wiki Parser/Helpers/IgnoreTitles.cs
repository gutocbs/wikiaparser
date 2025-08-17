using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Helpers;

public static class IgnoreListHelper
{
    public sealed class IgnoreConfig
    {
        [JsonProperty("titles")]
        public List<string> Titles { get; set; } = new();

        [JsonProperty("keywords")]
        public List<string> Keywords { get; set; } = new();
    }

    public static (HashSet<string> Titles, List<string> Keywords) Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Arquivo de ignore não encontrado: {path}");

        var json = File.ReadAllText(path);
        var config = JsonConvert.DeserializeObject<IgnoreConfig>(json)
                     ?? new IgnoreConfig();

        var titleSet = new HashSet<string>(config.Titles, StringComparer.OrdinalIgnoreCase);
        var keywordList = config.Keywords;

        return (titleSet, keywordList);
    }

    public static bool ShouldIgnore(string title, HashSet<string> ignoreTitles, List<string> ignoreKeywords)
    {
        if (string.IsNullOrEmpty(title))
            return false;

        // Match exato
        // if (ignoreTitles.Contains(title))
        
        //Contém palavra-chave
        if (ignoreTitles.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Contém palavra-chave
        if (ignoreKeywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }
    
    public static bool ShouldIgnore(string title, List<string> ignoreKeywords)
    {
        if (string.IsNullOrEmpty(title))
            return false;

        // Contém palavra-chave
        if (ignoreKeywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }
}