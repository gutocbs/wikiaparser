using System.Text.Json;
using Genshin.Wiki.Parser.Services.Sink;

namespace Genshin.Wiki.Parser.Tests.Sink;

[TestFixture]
public class SharedArraySinkTests
{
    private string _outDir = null!;

    [SetUp]
    public void SetUp()
    {
        _outDir = Path.Combine(Path.GetTempPath(), "wikiaparser-sink-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_outDir)) Directory.Delete(_outDir, recursive: true);
    }

    private SharedArraySink<object> CreateSink(long maxBytes = 1_000_000, int maxWords = 1_000_000, int maxFiles = 50,
        SharedArraySink<object>.OversizeItemBehavior behavior = SharedArraySink<object>.OversizeItemBehavior.AllowSingleFile)
        => new(_outDir, "items", maxBytes, maxWords, "txt", maxFiles, behavior);

    private string[] OutputFiles() => Directory.GetFiles(_outDir).OrderBy(f => f, StringComparer.Ordinal).ToArray();

    [Test]
    public void Should_WriteSingleValidJsonArray_When_ItemsFitInOneFile()
    {
        using (SharedArraySink<object> sink = CreateSink())
        {
            sink.Write(new { Name = "a" });
            sink.Write(new { Name = "b" });
        }

        string[] files = OutputFiles();
        Assert.That(files.Select(Path.GetFileName), Is.EqualTo(new[] { "items_01.txt" }));
        Assert.That(File.ReadAllText(files[0]), Is.EqualTo("[{\"Name\":\"a\"},{\"Name\":\"b\"}]"));
    }

    [Test]
    public void Should_SplitFiles_When_WordLimitIsExceeded()
    {
        using (SharedArraySink<object> sink = CreateSink(maxWords: 5))
        {
            sink.Write(new { Text = "one two" });    // 3 palavras (Text, one, two)
            sink.Write(new { Text = "three four" }); // 3 + 3 > 5 → novo arquivo
            sink.Write(new { Text = "x" });          // 3 + 2 = 5 → cabe no 2º arquivo
        }

        string[] files = OutputFiles();
        Assert.That(files, Has.Length.EqualTo(2));
        Assert.That(File.ReadAllText(files[0]), Is.EqualTo("[{\"Text\":\"one two\"}]"));
        Assert.That(File.ReadAllText(files[1]), Is.EqualTo("[{\"Text\":\"three four\"},{\"Text\":\"x\"}]"));
    }

    [Test]
    public void Should_SplitFiles_When_ByteLimitIsExceeded()
    {
        using (SharedArraySink<object> sink = CreateSink(maxBytes: 30))
        {
            sink.Write(new { V = "aaaaaaaaaa" }); // "[" + 18 bytes + "]" reservado = 20
            sink.Write(new { V = "bbbbbbbbbb" }); // 19 + "," + 18 + "]" = 39 > 30 → novo arquivo
        }

        string[] files = OutputFiles();
        Assert.That(files, Has.Length.EqualTo(2));
        foreach (string file in files)
            Assert.DoesNotThrow(() => JsonDocument.Parse(File.ReadAllText(file)));
    }

    [Test]
    public void Should_WriteOversizeItemAlone_When_BehaviorIsAllowSingleFile()
    {
        using (SharedArraySink<object> sink = CreateSink(maxWords: 2))
        {
            sink.Write(new { Text = "one two three" });
        }

        Assert.That(File.ReadAllText(OutputFiles().Single()), Is.EqualTo("[{\"Text\":\"one two three\"}]"));
    }

    [Test]
    public void Should_Throw_When_OversizeItemAndBehaviorIsThrow()
    {
        using SharedArraySink<object> sink = CreateSink(maxWords: 2, behavior: SharedArraySink<object>.OversizeItemBehavior.Throw);

        Assert.Throws<InvalidOperationException>(() => sink.Write(new { Text = "one two three" }));
    }

    [Test]
    public void Should_Throw_When_MaxFileCountIsReached()
    {
        using SharedArraySink<object> sink = CreateSink(maxWords: 2, maxFiles: 1);
        sink.Write(new { A = "x" });

        Assert.Throws<InvalidOperationException>(() => sink.Write(new { B = "y" }));
    }

    [Test]
    public void Should_CountWordsOnEscapedJson_When_SplittingByWords()
    {
        using (SharedArraySink<object> sink = CreateSink(maxWords: 3))
        {
            sink.Write(new { T = "Khaenri'ah" }); // STJ escapa o apóstrofo (u0027) → T, Khaenri, u0027ah = 3 palavras
            sink.Write(new { T = "é" });          // escapado como u00E9 → T, u00E9 = 2 → 3 + 2 > 3 → novo arquivo
        }

        Assert.That(OutputFiles(), Has.Length.EqualTo(2));
    }

    [Test]
    public void Should_CreateNoFile_When_NothingIsWritten()
    {
        using (CreateSink()) { }

        Assert.That(OutputFiles(), Is.Empty);
    }
}
