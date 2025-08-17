using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Character;

namespace Genshin.Wiki.Parser.Parsers;

public static class CharacterParser
{
    // Chaves de “pasta” no DTO
    private static readonly HashSet<string> PlayableKeys = new(StringComparer.OrdinalIgnoreCase)
        { "quality", "weapon", "element" };

    private static readonly HashSet<string> CharacterInfoKeys = new(StringComparer.OrdinalIgnoreCase)
        { "realname","birthday","constellation","region","region2","regionNote2",
          "affiliation","affiliation2","affiliation3","dish","namecard",
          "obtainType","obtain","releaseDate" };

    private static readonly HashSet<string> TitleKeys = new(StringComparer.OrdinalIgnoreCase)
        { "title2","titleRef2" };

    private static readonly HashSet<string> FamilyKeys = new(StringComparer.OrdinalIgnoreCase)
        { "father","fatherNote","father2","fatherNote2","sibling","siblingNote" };

    public static bool ContainsCharacterTabs(string wikitext)
        => wikitext?.IndexOf("CharacterTabs", StringComparison.OrdinalIgnoreCase) >= 0;

    public static CharacterDto? TryParseCharacter(string wikitext)
    {
        if (string.IsNullOrWhiteSpace(wikitext) || !ContainsCharacterTabs(wikitext))
            return null;

        string? infobox = TextHelper.ExtractTemplateBlock(wikitext, "Character Infobox");
        if (infobox == null)
            return null;

        // Parse dos campos do infobox
        Dictionary<string, string> fields = TextHelper.ParseTemplateFields(infobox);

        // Montagem do DTO
        CharacterDto dto = new CharacterDto
        {
            Type = TextHelper.Get(fields, "type"),
            PlayableCharacterInformation = new PlayableCharacterInformationDto
            {
                quality = TextHelper.Get(fields, "quality"),
                weapon  = TextHelper.Get(fields, "weapon"),
                element = TextHelper.Get(fields, "element")
            },
            name = TextHelper.Get(fields, "name"),
            CharacterInformation = new CharacterInformationDto
            {
                realname      = TextHelper.Get(fields, "realname"),
                birthday      = TextHelper.Get(fields, "birthday"),
                constellation = TextHelper.Get(fields, "constellation"),
                region        = TextHelper.Get(fields, "region"),
                region2       = TextHelper.Get(fields, "region2"),
                regionNote2   = TextHelper.Get(fields, "regionNote2"),
                affiliation   = TextHelper.Get(fields, "affiliation"),
                affiliation2  = TextHelper.Get(fields, "affiliation2"),
                affiliation3  = TextHelper.Get(fields, "affiliation3"),
                dish          = TextHelper.Get(fields, "dish"),
                namecard      = TextHelper.Get(fields, "namecard"),
                obtainType    = TextHelper.Get(fields, "obtainType"),
                obtain        = TextHelper.NormalizeObtain(TextHelper.Get(fields, "obtain")),
                releaseDate   = TextHelper.Get(fields, "releaseDate"),
            },
            title = TextHelper.Get(fields, "title"),
            Titles = new TitlesDto
            {
                title2    = TextHelper.Get(fields, "title2"),
                titleRef2 = TextHelper.ExtractUrlOrText(TextHelper.Get(fields, "titleRef2"))
            },
            ancestry = TextHelper.Get(fields, "ancestry"),
            Family = new FamilyDto
            {
                father      = TextHelper.Get(fields, "father"),
                fatherNote  = TextHelper.Get(fields, "fatherNote"),
                father2     = TextHelper.Get(fields, "father2"),
                fatherNote2 = TextHelper.Get(fields, "fatherNote2"),
                sibling     = TextHelper.Get(fields, "sibling"),
                siblingNote = TextHelper.Get(fields, "siblingNote"),
            },
            Description = TextHelper.ExtractDescription(wikitext, infobox)
        };

        // Limpamos rótulos vazios (opcional)
        if (TextHelper.IsEmpty(dto.PlayableCharacterInformation)) dto.PlayableCharacterInformation = null;
        if (TextHelper.IsEmpty(dto.CharacterInformation)) dto.CharacterInformation = null;
        if (TextHelper.IsEmpty(dto.Titles)) dto.Titles = null;
        if (TextHelper.IsEmpty(dto.Family)) dto.Family = null;

        return dto;
    }
}
