using System.Runtime.CompilerServices;

namespace Genshin.Wiki.Parser.Tests.Support;

/// <summary>Resolve caminhos de fixtures e snapshots a partir do diretório-fonte do projeto de testes.</summary>
public static class TestPaths
{
    public static string ProjectDirectory { get; } = ResolveProjectDirectory();

    public static string Fixture(string name) => Path.Combine(ProjectDirectory, "Fixtures", name);

    public static string Snapshot(string relativePath) => Path.Combine(ProjectDirectory, "Snapshots", relativePath);

    private static string ResolveProjectDirectory([CallerFilePath] string callerFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(callerFile)!, ".."));
}
