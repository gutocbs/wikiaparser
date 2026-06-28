using System.Text.Json;
using System.Text.Json.Serialization;

namespace Genshin.Wiki.Parser.Settings;

public static class JsonConfigReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static T Load<T>(string fileName) where T : new()
    {
        string path = ResolveConfigPath(fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Arquivo de configuração não encontrado: {path}", path);
        }

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, Options) ?? new T();
    }

    private static string ResolveConfigPath(string fileName)
    {
        if (Path.IsPathRooted(fileName)) return fileName;
        return Path.Combine(AppContext.BaseDirectory, "Config", fileName);
    }
}
