using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Character;

namespace Genshin.Wiki.Parser.Parsers.Character;

public static class NpcParser
{
    private static readonly HashSet<string> FamilyKeys = new(StringComparer.OrdinalIgnoreCase)
        { "father", "sibling", "mother", "spouse", "child", "relative" };
    
    public static NpcDto? TryParse(string wikitext, string? title)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (!wikitext.Contains("{{Character Infobox", StringComparison.OrdinalIgnoreCase))
            return null;

        var box = TextHelper.ExtractTemplateBlock(wikitext, "Character Infobox");
        if (box is null) return null;

        var fields = TextHelper.ParseTemplateFields(box, "Character Infobox");

        string? type = TextHelper.CleanInline(TextHelper.Get(fields, "type"));
        // Se o tipo explicitamente diz NPC, seguimos; caso não tenha type, ainda dá pra aceitar (muitos NPCs têm).
        if (!string.IsNullOrWhiteSpace(type) && type.IndexOf("npc", StringComparison.OrdinalIgnoreCase) < 0)
        {
            // tem infobox de personagem, mas não é NPC → provavelmente Playable/Enemy/etc.
            return null;
        }

        var dto = new NpcDto
        {
            Name        = TextHelper.CleanInline(title),
            RealName    = TextHelper.CleanInline(TextHelper.Get(fields, "realname")),
            Type        = type,
            Element     = TextHelper.CleanInline(TextHelper.Get(fields, "element")),
            Region      = TextHelper.CleanInline(TextHelper.Get(fields, "region")),
            Locations   = TextHelper.ToList(TextHelper.Get(fields, "location")),
            Affiliations= TextHelper.ToList(TextHelper.Get(fields, "affiliation")),
            Title       = TextHelper.CleanInline(TextHelper.Get(fields, "title")),
            Deceased    = TextHelper.CleanInline(TextHelper.Get(fields, "deceased")),
            Family = ExtractFamily(fields),
            ShortDescription = TextHelper.ExtractDescriptionTemplate(wikitext),
            Profile     = TextHelper.ExtractSection(wikitext, "Profile"),
            Appearance  = TextHelper.ExtractSection(wikitext, "Appearance"),
        };

        // Se praticamente nada foi preenchido, evita poluir:
        bool hasCore =
            !string.IsNullOrWhiteSpace(dto.Name) ||
            !string.IsNullOrWhiteSpace(dto.Type);
        return hasCore ? dto : null;
    }
    
    private static List<DetailDto>? ExtractFamily(Dictionary<string, string> fields)
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
