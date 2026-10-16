using Genshin.Wiki.Parser.Settings;
using Genshin.Wiki.Parser.Tests.Support;

namespace Genshin.Wiki.Parser.Tests.Pipeline;

/// <summary>
/// Executa o pipeline completo sobre o mini-dump sintético e compara cada arquivo gerado com o snapshot.
/// É a rede de segurança principal das otimizações: a saída tem de continuar byte a byte igual.
/// </summary>
[TestFixture]
public class PipelineCharacterizationTests
{
    private string _outDir = null!;

    [SetUp]
    public void SetUp()
    {
        _outDir = Path.Combine(Path.GetTempPath(), "wikiaparser-pipeline-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_outDir)) Directory.Delete(_outDir, recursive: true);
    }

    // Único ponto que conhece a assinatura de entrada do pipeline. Usa os mesmos arquivos de configuração do programa.
    private static void RunPipeline(string dumpPath, string outputPath)
    {
        ParserRegistrationSettings registrations = JsonConfigReader.Load<ParserRegistrationSettings>(ConfigPath("parser-registrations.json"));
        ExportSettings exportSettings = JsonConfigReader.Load<ExportSettings>(ConfigPath("export-settings.json"));

        MediaWikiFilter.Process(dumpPath, AppPath("IgnoreList.json"), outputPath,
            registrations.ToParserRegistrations(), exportSettings);
    }

    private static string AppPath(string relativePath)
        => Path.GetFullPath(Path.Combine(TestPaths.ProjectDirectory, "..", "Genshin Wiki Parser", relativePath));

    private static string ConfigPath(string fileName) => AppPath(Path.Combine("Configuration", fileName));

    [Test]
    public void Should_ProduceSnapshotFiles_When_ProcessingMiniDump()
    {
        RunPipeline(TestPaths.Fixture("mini-dump.xml"), _outDir);

        string[] files = Directory.GetFiles(_outDir).Select(Path.GetFileName).OrderBy(f => f, StringComparer.Ordinal).ToArray()!;
        Snapshot.MatchText(string.Join("\n", files), "Pipeline/_files.txt");
        foreach (string file in files)
            Snapshot.MatchText(File.ReadAllText(Path.Combine(_outDir, file)), $"Pipeline/{file}");
    }

    [Test]
    public void Should_ProduceIdenticalOutput_When_RunTwice()
    {
        string secondOut = _outDir + "-2";
        try
        {
            RunPipeline(TestPaths.Fixture("mini-dump.xml"), _outDir);
            RunPipeline(TestPaths.Fixture("mini-dump.xml"), secondOut);

            foreach (string file in Directory.GetFiles(_outDir))
                Assert.That(File.ReadAllBytes(Path.Combine(secondOut, Path.GetFileName(file))),
                    Is.EqualTo(File.ReadAllBytes(file)), Path.GetFileName(file));
        }
        finally
        {
            if (Directory.Exists(secondOut)) Directory.Delete(secondOut, recursive: true);
        }
    }
}
