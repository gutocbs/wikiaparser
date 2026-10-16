using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Tests.Support;

namespace Genshin.Wiki.Parser.Tests.Helpers;

[TestFixture]
public class IgnoreListHelperTests
{
    private HashSet<string> _titles = null!;
    private List<string> _keywords = null!;

    [SetUp]
    public void SetUp()
    {
        _titles = new HashSet<string>(["Template:", "Gallery"], StringComparer.OrdinalIgnoreCase);
        _keywords = ["User:", "/@comment-"];
    }

    [TestCase("Template:Weapon Infobox", true)]
    [TestCase("template:lowercase", true)]
    [TestCase("Kaeya/Gallery", true)]
    [TestCase("User:Someone", true)]
    [TestCase("Talk/@comment-123", true)]
    [TestCase("Kaeya", false)]
    [TestCase("", false)]
    public void Should_MatchBySubstring_When_CheckingTitle(string title, bool expected)
    {
        bool result = IgnoreListHelper.ShouldIgnore(title, _titles, _keywords);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("Written by [[User:Someone]].", true)]
    [TestCase("written by [[user:someone]].", true)]
    [TestCase("Template: is not a keyword", false)]
    [TestCase(null, false)]
    public void Should_MatchKeywordsOnly_When_CheckingContent(string? content, bool expected)
    {
        bool result = IgnoreListHelper.ShouldIgnore(content!, _keywords);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Should_LoadTitlesAndKeywords_When_ReadingProjectIgnoreList()
    {
        string path = Path.Combine(TestPaths.ProjectDirectory, "..", "Genshin Wiki Parser", "IgnoreList.json");

        (HashSet<string> titles, List<string> keywords) = IgnoreListHelper.Load(path);

        Assert.Multiple(() =>
        {
            Assert.That(titles.Contains("template:"), Is.True, "comparação de títulos é case-insensitive");
            Assert.That(keywords, Does.Contain("User:"));
        });
    }

    [Test]
    public void Should_Throw_When_IgnoreListDoesNotExist()
    {
        Assert.Throws<FileNotFoundException>(() => IgnoreListHelper.Load("does-not-exist.json"));
    }
}
