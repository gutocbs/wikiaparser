using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Services;
using Genshin.Wiki.Parser.Tests.Support;

namespace Genshin.Wiki.Parser.Tests.Helpers;

[TestFixture]
public class MediaWikiPageReaderTests
{
    private string _dumpPath = null!;

    [SetUp]
    public void SetUp()
    {
        _dumpPath = Path.Combine(Path.GetTempPath(), "wikiaparser-dump-" + Guid.NewGuid().ToString("N") + ".xml");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_dumpPath)) File.Delete(_dumpPath);
    }

    private List<Page> Read(string pagesXml, Func<string, bool>? shouldSkipTitle = null)
    {
        File.WriteAllText(_dumpPath,
            "<mediawiki xmlns=\"http://www.mediawiki.org/xml/export-0.11/\" version=\"0.11\">\n" +
            "<siteinfo><sitename>Test</sitename><namespaces><namespace key=\"0\" /></namespaces></siteinfo>\n" +
            pagesXml + "\n</mediawiki>");
        return MediaWikiPageReader.ReadPages(_dumpPath, shouldSkipTitle).ToList();
    }

    [Test]
    public void Should_ReadTitleIdAndText_When_PageHasOneRevision()
    {
        List<Page> pages = Read("""
            <page>
              <title>Kaeya</title>
              <ns>0</ns>
              <id>5</id>
              <revision>
                <id>99</id>
                <contributor><username>U</username><id>7</id></contributor>
                <text bytes="3" xml:space="preserve">abc</text>
                <sha1>x</sha1>
              </revision>
            </page>
            """);

        Page page = pages.Single();
        Assert.Multiple(() =>
        {
            Assert.That(page.title, Is.EqualTo("Kaeya"));
            Assert.That(page.id, Is.EqualTo("5"), "id da página, não da revisão nem do contributor");
            Assert.That(page.revision.text.content, Is.EqualTo("abc"));
            Assert.That(page.About, Is.Null);
        });
    }

    [Test]
    public void Should_DecodeEntitiesAndKeepWhitespace_When_ReadingText()
    {
        List<Page> pages = Read(
            "<page>\n<title>A &amp; B</title>\n<revision>\n" +
            "<text xml:space=\"preserve\">  &lt;ref&gt;x&lt;/ref&gt; &amp;mdash;\r\nline2  </text>\n</revision>\n</page>");

        Assert.That(pages.Single().title, Is.EqualTo("A & B"));
        Assert.That(pages.Single().revision.text.content, Is.EqualTo("  <ref>x</ref> &mdash;\nline2  "));
    }

    [Test]
    public void Should_ReturnEmptyText_When_TextElementIsEmpty()
    {
        List<Page> pages = Read("<page>\n<title>Empty</title>\n<revision>\n<text bytes=\"0\" />\n</revision>\n</page>");

        Assert.That(pages.Single().revision.text.content, Is.Empty);
    }

    [Test]
    public void Should_ReturnEmptyText_When_PageHasNoRevision()
    {
        List<Page> pages = Read("<page>\n<title>No revision</title>\n<redirect title=\"X\" />\n</page>");

        Assert.That(pages.Single().revision.text.content, Is.Empty);
    }

    [Test]
    public void Should_UseLastRevision_When_PageHasSeveralRevisions()
    {
        List<Page> pages = Read(
            "<page>\n<title>History</title>\n<revision>\n<text>old</text>\n</revision>\n<revision>\n<text>new</text>\n</revision>\n</page>");

        Assert.That(pages.Single().revision.text.content, Is.EqualTo("new"));
    }

    [Test]
    public void Should_SkipPage_When_TitleIsEmpty()
    {
        List<Page> pages = Read("<page>\n<title></title>\n<revision>\n<text>x</text>\n</revision>\n</page>");

        Assert.That(pages, Is.Empty);
    }

    [Test]
    public void Should_SkipPageWithoutReadingRevision_When_TitlePredicateMatches()
    {
        List<Page> pages = Read(
            "<page>\n<title>Template:X</title>\n<revision>\n<text>1</text>\n</revision>\n</page>\n" +
            "<page>\n<title>Kept</title>\n<revision>\n<text>2</text>\n</revision>\n</page>",
            title => title.StartsWith("Template:", StringComparison.Ordinal));

        Assert.That(pages.Select(p => p.title), Is.EqualTo(new[] { "Kept" }));
    }

    [Test]
    public void Should_KeepDumpOrder_When_ReadingSeveralPages()
    {
        List<Page> pages = Read(
            "<page>\n<title>First</title>\n<revision>\n<text>1</text>\n</revision>\n</page>\n" +
            "<page>\n<title>Second</title>\n<revision>\n<text>2</text>\n</revision>\n</page>\n" +
            "<page>\n<title>Third</title>\n<revision>\n<text>3</text>\n</revision>\n</page>");

        Assert.That(pages.Select(p => p.title), Is.EqualTo(new[] { "First", "Second", "Third" }));
    }

    [Test]
    public void Should_ReadRevision_When_ElementsHaveNoWhitespaceBetweenThem()
    {
        // XML válido sem quebras de linha entre os elementos (dump "minificado").
        List<Page> pages = Read("<page><title>Compact</title><revision><text>abc</text></revision></page>");

        Assert.That(pages.Single().revision.text.content, Is.EqualTo("abc"));
    }

    [Test]
    public void Should_ReadAllFixturePages_When_ReadingMiniDump()
    {
        List<Page> pages = MediaWikiPageReader.ReadPages(TestPaths.Fixture("mini-dump.xml")).ToList();

        Assert.That(pages, Has.Count.EqualTo(29));
        Assert.That(pages[0].title, Is.EqualTo("Amber/Lore"));
        Assert.That(pages[^1].title, Is.EqualTo("Teyvat"));
    }
}
