using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Genshin.Wiki.Parser.Services;

public sealed class OutputSink : IDisposable
{
    private readonly FileStream _fs;
    private readonly StreamWriter _sw;
    public readonly JsonTextWriter Writer;
    public bool FirstItem = true;

    public OutputSink(string path, Formatting formatting = Formatting.Indented)
    {
        EnsureParentDirectory(path);

        _fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        _sw = new StreamWriter(_fs);
        Writer = new JsonTextWriter(_sw) { Formatting = formatting };
        Writer.WriteStartArray();
    }

    private static void EnsureParentDirectory(string filePath)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
        // se por acaso existe um arquivo no lugar do diretório-pai, explodimos com mensagem clara
        if (File.Exists(parent))
            throw new IOException($"O caminho de saída '{parent}' existe como arquivo. Passe um diretório válido para outputDir.");
        Directory.CreateDirectory(parent);
    }

    public void Write(JsonSerializer serializer, object dto)
    {
        serializer.Serialize(Writer, dto);
    }

    public void Dispose()
    {
        Writer.WriteEndArray();
        Writer.Flush();
        _sw.Flush();
        _sw.Dispose();
        _fs.Dispose();
    }
}