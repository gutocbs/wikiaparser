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
        
        string? infobox = TextHelper.ExtractTemplateBlock(wikitext, WikiTemplates.CharacterInfobox);
        if (infobox == null)
            return null;

        // Parse dos campos do infobox
        Dictionary<string, string> fields = TextHelper.ParseTemplateFields(infobox);

        // Montagem do DTO
        PlayableCharacterDto dto = new PlayableCharacterDto
        {
            Type = TextHelper.Get(fields, CommonFieldNames.Type),
            PlayableCharacterInformation = new PlayableCharacterInformationDto
            {
                Quality = TextHelper.Get(fields, CommonFieldNames.Quality),
                Weapon  = TextHelper.Get(fields, CharacterFieldNames.Weapon),
                Element = TextHelper.Get(fields, CharacterFieldNames.Element)
            },
            Name = TextHelper.Get(fields, CharacterFieldNames.Name),
            CharacterInformation = new CharacterInformationDto
            {
                RealName      = TextHelper.Get(fields, CharacterFieldNames.RealName),
                Birthday      = TextHelper.Get(fields, CharacterFieldNames.Birthday),
                Constellation = TextHelper.Get(fields, CharacterFieldNames.Constellation),
                Regions       = ExtractDetailsList(fields, CommonFieldNames.Region),
                Affiliations  = ExtractDetailsList(fields, CharacterFieldNames.Affiliation),
                Dish          = TextHelper.Get(fields, CharacterFieldNames.Dish),
                Namecard      = TextHelper.Get(fields, CharacterFieldNames.Namecard),
                ObtainType    = TextHelper.Get(fields, CharacterFieldNames.ObtainType),
                Obtain        = TextHelper.NormalizeObtain(TextHelper.Get(fields, CharacterFieldNames.Obtain)),
                ReleaseDate   = TextHelper.Get(fields, CharacterFieldNames.ReleaseDate),
            },
            Titles = ExtractDetailsList(fields, CommonFieldNames.Title),
            Ancestry = TextHelper.Get(fields, CharacterFieldNames.Ancestry),
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
            foreach (KeyValuePair<string, string> keyValuePair in dictionary)
            {
                string key = keyValuePair.Key;
                string? value = TextHelper.Get(fields, keyValuePair.Key);
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
            foreach (KeyValuePair<string, string> keyValuePair in dictionary)
            {
                string key = keyValuePair.Key;
                string? value = TextHelper.Get(fields, keyValuePair.Key);
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
            foreach (KeyValuePair<string, string> family in familyFields)
            {
                string familyKey = family.Key;
                string? familyValue = TextHelper.Get(fields, family.Key);
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
