using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Tests.Support;

namespace Genshin.Wiki.Parser.Tests.Helpers;

/// <summary>
/// Characterization tests das primitivas de wikitext: cada teste aplica a função a uma tabela de entradas
/// e compara com a saída registrada em snapshot. Qualquer otimização deve manter os snapshots idênticos.
/// </summary>
[TestFixture]
public class TextHelperCharacterizationTests
{
    private static readonly string?[] MarkupInputs =
    [
        null,
        "",
        "   ",
        "plain text",
        "[[Target|Display]] and [[Simple]]",
        "{{Cryo}} and {{Color|Pyro}}",
        "nested {{sic|[[Akasha]]}}",
        "deep {{A|{{B|c}}}} nesting",
        "'''bold''' ''italic'' '''''both'''''",
        "a<!-- hidden -->b<!-- multi\nline -->c",
        "text<ref>source</ref> more <ref name=\"x\">http://example.org/ref</ref> end",
        "self closing <ref name=\"x\" /> ref",
        "line<br>break<br/>again<br />end",
        "<p>para one</p><p>para two</p>",
        "<span class=\"x\">tag</span> removed",
        "[http://example.org Link text] and [https://example.org/b Other]",
        "[:Category:Foo]",
        "a &mdash; b &ndash; c",
        "a  \n\n\n\nb   \nc",
        "* bullet one\n* bullet two\n  *  bullet three",
        "Cataclysm|destruction",
        "<small>tiny</small> text",
        "Character Infobox leftover",
        "mixed {{Color|Anemo|[[Link]]}} '''x''' <br> [[A|B]]",
    ];

    private static List<object> Table(Func<string?, string?> function)
        => MarkupInputs.Select(input => (object)new { Input = input, Output = function(input) }).ToList();

    [Test]
    public void Should_MatchSnapshot_When_CleaningText()
        => Snapshot.MatchJson(Table(TextHelper.CleanText), "TextHelper/CleanText.json");

    [Test]
    public void Should_MatchSnapshot_When_CleaningInline()
        => Snapshot.MatchJson(Table(TextHelper.CleanInline), "TextHelper/CleanInline.json");

    [Test]
    public void Should_MatchSnapshot_When_CleaningValue()
        => Snapshot.MatchJson(Table(TextHelper.CleanValue), "TextHelper/CleanValue.json");

    [Test]
    public void Should_MatchSnapshot_When_CleaningCell()
        => Snapshot.MatchJson(Table(s => s is null ? null : TextHelper.CleanCell(s)), "TextHelper/CleanCell.json");

    [Test]
    public void Should_MatchSnapshot_When_ExtractingTemplateBlocks()
    {
        (string Text, string Name)[] cases =
        [
            ("{{Weapon Infobox\n|id = 1\n}}", "Weapon Infobox"),
            ("before {{ weapon infobox |a=1}} after", "Weapon Infobox"),
            ("{{Weapon Infobox\n|a = {{Color|x}}\n|b = {{A|{{B}}}}\n}} tail {{Other}}", "Weapon Infobox"),
            ("{{Weapon InfoboxExtra|a=1}}", "Weapon Infobox"),
            ("no template here", "Weapon Infobox"),
            ("{{Weapon Infobox\n|unbalanced = {{x}}", "Weapon Infobox"),
            ("{{Character Infobox|a=1}}{{Character Infobox|a=2}}", "Character Infobox"),
            ("{{VO/Story\n|vo_01_01_tx = hi\n}}", "VO/Story"),
        ];

        Snapshot.MatchJson(
            cases.Select(c => new { c.Text, c.Name, Output = TextHelper.ExtractTemplateBlock(c.Text, c.Name) }),
            "TextHelper/ExtractTemplateBlock.json");
    }

    [Test]
    public void Should_MatchSnapshot_When_ParsingTemplateFieldsWithHeader()
    {
        string[] cases =
        [
            "Weapon Infobox\n|id = 11401\n|type = [[Sword]]\n|effect = line one\nline two\n|empty =\n|noequals\n|after = ok",
            "Weapon Infobox\n|key <!-- c --> = value<!-- x -->\n|Key = duplicate key\n| spaced = leading space",
            "weapon infobox|inline = 1\n|image = <gallery>\nA.png|Base\n</gallery>\n|last = x",
        ];

        Snapshot.MatchJson(
            cases.Select(c => new { Input = c, Output = TextHelper.ParseTemplateFields(c, "Weapon Infobox") }),
            "TextHelper/ParseTemplateFieldsWithHeader.json");
    }

    [Test]
    public void Should_MatchSnapshot_When_ParsingCharacterInfoboxFields()
    {
        string[] cases =
        [
            "Character Infobox\n|image = <gallery>\nA.png|Card\n</gallery>\n|name = Kaeya\n|obtain = * [[A]]\n* [[B]]",
            "Character Infobox\n|image = Single.png\n|name = Amber\n|title2 = '''x'''<!-- c -->",
            "|a = 1\n |b = not a field\n|c=3",
        ];

        Snapshot.MatchJson(
            cases.Select(c => new { Input = c, Output = TextHelper.ParseTemplateFields(c) }),
            "TextHelper/ParseTemplateFieldsCharacter.json");
    }

    [Test]
    public void Should_MatchSnapshot_When_ExtractingSections()
    {
        const string text = "Intro\n==Profile==\nProfile text '''bold'''.\n===Sub===\nSub text\n== Appearance ==\nLooks.\n==Other==\nEnd";
        string[] headings = ["Profile", "Sub", "Appearance", "Other", "Missing", "profile"];

        Snapshot.MatchJson(
            headings.Select(h => new { Heading = h, Output = TextHelper.ExtractSection(text, h) }),
            "TextHelper/ExtractSection.json");
    }

    [Test]
    public void Should_MatchSnapshot_When_ExtractingTemplatesAndParams()
    {
        const string text = "{{Item Infobox\n|id = 1\n|type = Food\n|description = Uses {{Color|x}} inside\n|source1 = A\n}}\n{{Quest Description|One}}\n{{Quest Description|Two}}";

        string? body = TextHelper.ExtractTemplate("Item Infobox", text);
        Snapshot.MatchJson(new
        {
            Body = body,
            Params = TextHelper.ParseTemplateParams(body ?? ""),
            Missing = TextHelper.ExtractTemplate("Missing", text),
            Templates = TextHelper.ExtractTemplates("Quest Description", text).ToList(),
        }, "TextHelper/ExtractTemplateAndParams.json");
    }

    [Test]
    public void Should_MatchSnapshot_When_UsingSmallHelpers()
    {
        string?[] titles = [null, "  ", "Kaeya", "Kaeya/Lore", "Kaeya: Pavo Ocellus", "Template:X/doc", " Amber / Companion "];
        string?[] lists = [null, "", "[[A]], [[B]]; C / D | a", "x,,y"];

        Snapshot.MatchJson(new
        {
            BaseKeys = titles.Select(t => new { Input = t, Output = TextHelper.GetBaseKey(t) }),
            Obtain = new[] { null, "* [[A]]\n* [[B]] (Wish)", "* A * B", "A; B | C\nD" }
                .Select(o => new { Input = o, Output = TextHelper.NormalizeObtain(o) }),
            Lists = lists.Select(l => new { Input = l, Output = TextHelper.ToList(l) }),
            Descriptions = new[] { "{{Description|A [[link]] text.}}", "{{Description}}", "none" }
                .Select(d => new { Input = d, Output = TextHelper.ExtractDescriptionTemplate(d) }),
        }, "TextHelper/SmallHelpers.json");
    }

    [Test]
    public void Should_IgnoreReplacementTable_When_CleaningInline()
    {
        // Quirk atual: CleanInline chama ReplaceText mas descarta o resultado.
        string? result = TextHelper.CleanInline("Cataclysm|destruction");

        Assert.That(result, Is.EqualTo("Cataclysm|destruction"));
    }

    [Test]
    public void Should_StopAtFirstNestedClose_When_ExtractingTemplateNonGreedy()
    {
        // Quirk atual: ExtractTemplate não balanceia chaves e corta no primeiro "}}".
        string? body = TextHelper.ExtractTemplate("Item Infobox", "{{Item Infobox|a = {{X}}|b = 2}}");

        Assert.That(body, Is.EqualTo("|a = {{X"));
    }
}
