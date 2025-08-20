using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Character;

namespace Genshin.Wiki.Parser.Parsers.Character;

public static class CharacterParser
{
    private static readonly HashSet<string> IgnoreKeys = new(StringComparer.OrdinalIgnoreCase)
        { "formerly", "in lore" };

    private static readonly HashSet<string> FamilyKeys = new(StringComparer.OrdinalIgnoreCase)
        { "father", "sibling", "mother", "spouse", "child", "relative" };

    public static PlayableCharacterDto? TryParse(string wikitext)
    {
        if (string.IsNullOrWhiteSpace(wikitext))
            return null;
        
        string? infobox = TextHelper.ExtractTemplateBlock(wikitext, "Character Infobox");
        if (infobox == null)
            return null;

        // Parse dos campos do infobox
        Dictionary<string, string> fields = TextHelper.ParseTemplateFields(infobox);

        // Montagem do DTO
        PlayableCharacterDto dto = new PlayableCharacterDto
        {
            Type = TextHelper.Get(fields, "type"),
            PlayableCharacterInformation = new PlayableCharacterInformationDto
            {
                Quality = TextHelper.Get(fields, "quality"),
                Weapon  = TextHelper.Get(fields, "weapon"),
                Element = TextHelper.Get(fields, "element")
            },
            Name = TextHelper.Get(fields, "name"),
            CharacterInformation = new CharacterInformationDto
            {
                RealName      = TextHelper.Get(fields, "realname"),
                Birthday      = TextHelper.Get(fields, "birthday"),
                Constellation = TextHelper.Get(fields, "constellation"),
                Regions       = ExtractDetailsList(fields, "region"),
                Affiliations  = ExtractDetailsList(fields, "affiliation"),
                Dish          = TextHelper.Get(fields, "dish"),
                Namecard      = TextHelper.Get(fields, "namecard"),
                ObtainType    = TextHelper.Get(fields, "obtainType"),
                Obtain        = TextHelper.NormalizeObtain(TextHelper.Get(fields, "obtain")),
                ReleaseDate   = TextHelper.Get(fields, "releaseDate"),
            },
            Titles = ExtractDetailsList(fields, "title"),
            Ancestry = TextHelper.Get(fields, "ancestry"),
            Family = ExtractFamily(fields),
            Description = TextHelper.ExtractDescription(wikitext, infobox)
        };

        // Limpamos rótulos vazios (opcional)
        if (TextHelper.IsEmpty(dto.PlayableCharacterInformation)) dto.PlayableCharacterInformation = null;
        if (TextHelper.IsEmpty(dto.CharacterInformation)) dto.CharacterInformation = null;
        if (TextHelper.IsEmpty(dto.Family)) dto.Family = null;

        return dto;
    }

    private static List<DetailDto> ExtractDetails(Dictionary<string, string?> fields, string detailKey, bool ignoreUrl = true)
    {
        Dictionary<string, string> dictionary = fields
            .Where(kvp => IgnoreKeys.Any(t => kvp.Key.Contains(detailKey) && !kvp.Key.Contains(t, StringComparison.OrdinalIgnoreCase)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, fields.Comparer);
        
        List<DetailDto> detailDtos = new List<DetailDto>();
        
        if(dictionary.Count > 0)
        {
            foreach (var keyValuePair in dictionary)
            {
                var key = keyValuePair.Key;
                var value = TextHelper.Get(fields, keyValuePair.Key);
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    if(ignoreUrl && TextHelper.IsUrl(value))
                        continue;
                    
                    detailDtos.Add(new DetailDto
                    {
                        Title = TextHelper.CleanInline(key),
                        Note = TextHelper.IsUrl(value) ? TextHelper.ExtractUrlOrText(value) : TextHelper.CleanInline(value)
                    });
                }
            }
        }

        return detailDtos;
    }
    
    private static List<string?> ExtractDetailsList(Dictionary<string, string?> fields, string detailKey, bool ignoreUrl = true)
    {
        Dictionary<string, string> dictionary = fields
            .Where(kvp => kvp.Key.Contains(detailKey) && !IgnoreKeys.Any(t => kvp.Value.Contains(t, StringComparison.OrdinalIgnoreCase)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, fields.Comparer);
        
        List<string?> detailDto = [];
        
        if(dictionary.Count > 0)
        {
            foreach (var keyValuePair in dictionary)
            {
                var key = keyValuePair.Key;
                var value = TextHelper.Get(fields, keyValuePair.Key);
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    if(ignoreUrl && TextHelper.IsUrl(value))
                        continue;
                    
                    detailDto.Add(TextHelper.IsUrl(value) ? TextHelper.ExtractUrlOrText(value) : TextHelper.CleanInline(value));
                }
            }
        }

        return detailDto;
    }
    
    private static List<DetailDto>? ExtractFamily(Dictionary<string, string?> fields)
    {
        Dictionary<string, string> familyFields = fields
            .Where(kvp => FamilyKeys.Any(t => kvp.Key.Contains(t, StringComparison.OrdinalIgnoreCase)) && !kvp.Key.Contains("Ref"))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, fields.Comparer);
        
        List<DetailDto>? familyDto = new List<DetailDto>();
        if(familyFields.Count > 0)
        {
            foreach (var family in familyFields)
            {
                var familyKey = family.Key;
                var familyValue = TextHelper.Get(fields, family.Key);
                if (!string.IsNullOrWhiteSpace(familyKey) && !string.IsNullOrWhiteSpace(familyValue))
                {
                    familyDto.Add(new DetailDto
                    {
                        Title = TextHelper.CleanInline(familyKey),
                        Note = TextHelper.CleanInline(familyValue)
                    });
                }
            }
        }
        else
            return null;

        return familyDto;
    }
}
