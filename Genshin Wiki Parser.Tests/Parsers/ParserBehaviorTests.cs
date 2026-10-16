using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Character;
using Genshin.Wiki.Parser.Models.Location;
using Genshin.Wiki.Parser.Models.XML;
using Genshin.Wiki.Parser.Parsers.Character;
using Genshin.Wiki.Parser.Parsers.Location;
using Genshin.Wiki.Parser.Services;

namespace Genshin.Wiki.Parser.Tests.Parsers;

/// <summary>
/// Fixa comportamentos atuais relevantes (inclusive quirks conhecidos) para que as otimizações não os alterem
/// sem intenção. Testes marcados "Quirk" documentam bugs conhecidos que ficaram fora do escopo de performance.
/// </summary>
[TestFixture]
public class ParserBehaviorTests
{
    private PlayableCharacterService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new PlayableCharacterService();
    }

    private static Page CreatePage(string title, string text)
        => new() { title = title, revision = new Revision { text = new Text { content = text } } };

    private bool Set(Page page) => _service.Set(page, page.revision.text.content, TextHelper.GetBaseKey(page.title));

    private const string AmberPage = "{{Character Tabs}}\n{{Character Infobox\n|name = Amber\n|type = Playable\n}}\n'''Amber''' was an Outrider.";
    private const string LorePage = "{{Character Tabs}}\n==Personality==\nCheerful.";
    private const string NamecardPage = "{{Character Tabs}}\n{{Item Infobox\n|id = 1\n|type = Namecard\n|description = Card.\n}}\nIt is a [[Namecard]] obtained by friendship.";

    [Test]
    public void Should_AttachLore_When_LorePageComesBeforeCharacter()
    {
        Page lore = CreatePage("Amber/Lore", LorePage);
        Page amber = CreatePage("Amber", AmberPage);

        Set(lore);
        Set(amber);

        var character = (PlayableCharacterDto)amber.About!;
        Assert.That(character.Lore?.Personality, Is.EqualTo("Cheerful."));
        Assert.That(lore.About, Is.Null, "subpáginas não geram entidade própria");
    }

    [Test]
    public void Should_AttachLore_When_LorePageComesAfterCharacter()
    {
        Page amber = CreatePage("Amber", AmberPage);
        Set(amber);

        Set(CreatePage("Amber/Lore", LorePage));

        Assert.That(((PlayableCharacterDto)amber.About!).Lore?.Personality, Is.EqualTo("Cheerful."));
    }

    [Test]
    public void Should_AttachNamecard_When_NamecardComesAfterCharacter()
    {
        Page amber = CreatePage("Amber", AmberPage);
        Set(amber);

        Set(CreatePage("Amber: Outrider", NamecardPage));

        Assert.That(((PlayableCharacterDto)amber.About!).Namecard?.Title, Is.EqualTo("Outrider"));
    }

    [Test]
    public void Quirk_Should_NotAttachNamecard_When_NamecardComesBeforeCharacter()
    {
        // Bug conhecido: o namecard pendente é guardado com a chave "Outrider" em vez de "Amber".
        Set(CreatePage("Amber: Outrider", NamecardPage));
        Page amber = CreatePage("Amber", AmberPage);

        Set(amber);

        Assert.That(((PlayableCharacterDto)amber.About!).Namecard, Is.Null);
    }

    [Test]
    public void Should_RequireCharacterTabs_When_ParsingPlayableCharacters()
    {
        Page page = CreatePage("Amber", AmberPage.Replace("{{Character Tabs}}", ""));

        bool parsed = Set(page);

        Assert.That(parsed, Is.False);
    }

    [Test]
    public void Quirk_Should_ReturnNull_When_CompanionPageHasDialogueSections()
    {
        // Bug conhecido: ExtractSection limpa "{{Dialogue Start}}" antes de procurá-lo, então nada é encontrado.
        const string text = "==Idle Quotes==\n{{Dialogue Start}}\n:{{DIcon|Idle}} Hey!\n{{Dialogue End}}\n==Dialogue==\n{{Dialogue Start}}\n:'''Amber:''' Hi\n{{Dialogue End}}";

        CompanionDto? companion = CompanionParser.TryParse(text, "Amber/Companion");

        Assert.That(companion, Is.Null);
    }

    [Test]
    public void Quirk_Should_KeepOnlyFirstNpc_When_LocationListsSeveralNpcs()
    {
        // Bug conhecido: o "$" multiline faz a seção ==NPCs== terminar na primeira linha.
        const string text = "{{Location Infobox\n|type = Tavern\n}}\n==NPCs==\n* [[Charles]]\n* [[Diluc]]\n==Descriptions==\n{{Description|One.|Map}}\n{{Description|Two.|Other}}";

        LocationDto? location = LocationParser.TryParse(text, "Angel's Share");

        Assert.That(location!.Npcs, Is.EqualTo(new[] { "Charles" }));
        Assert.That(location.Descriptions.Select(d => d.Text), Is.EqualTo(new[] { "One." }));
    }
}
