using System.Text.Json;

namespace Genshin.Wiki.Parser.Tests.Support;

/// <summary>
/// Characterization snapshots: registram a saída ATUAL do parser e falham se ela mudar.
/// Para (re)gerar de propósito: defina a variável de ambiente UPDATE_SNAPSHOTS=1 e rode os testes.
/// </summary>
public static class Snapshot
{
    // Mesmas opções usadas pelo exportador (MultiSinkExporter), só que indentado para leitura humana.
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static bool ShouldUpdate =>
        string.Equals(Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS"), "1", StringComparison.Ordinal);

    public static void MatchJson(object? value, string relativePath)
        => MatchText(JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object), JsonOptions), relativePath);

    public static void MatchText(string actual, string relativePath)
    {
        string path = TestPaths.Snapshot(relativePath);
        string normalized = actual.Replace("\r\n", "\n");

        if (ShouldUpdate || !File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, normalized);
            if (!ShouldUpdate)
                Assert.Fail($"Snapshot criado em '{relativePath}'. Revise o arquivo e rode os testes de novo.");
            return;
        }

        string expected = File.ReadAllText(path).Replace("\r\n", "\n");
        Assert.That(normalized, Is.EqualTo(expected), $"Saída diferente do snapshot '{relativePath}'.");
    }
}
