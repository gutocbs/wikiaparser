using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Item;

namespace Genshin.Wiki.Parser.Parsers.Item;

public static class ItemParser
{
    public static ItemDto? TryParse(string wikiText, string pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikiText)) return null;
        if (!wikiText.Contains($"{WikiSyntax.TemplateOpen}{WikiTemplates.ItemInfobox}", StringComparison.OrdinalIgnoreCase)) return null;

        ItemDto dto = new ItemDto { Title = pageTitle };

        // 1) Infobox
        string infobox = TextHelper.ExtractTemplate(WikiTemplates.ItemInfobox, wikiText);
        Dictionary<string, string> map  = TextHelper.ParseTemplateParams(infobox);

        dto.Id          = TextHelper.TryInt(TextHelper.Get(map, CommonFieldNames.Id));
        dto.Type        = TextHelper.Get(map, CommonFieldNames.Type);
        dto.Group       = TextHelper.Get(map, CommonFieldNames.Group);
        dto.Quality     = TextHelper.TryInt(TextHelper.Get(map, CommonFieldNames.Quality)) ?? TextHelper.TryInt(TextHelper.Get(map, ItemFieldNames.Rarity)) ?? TextHelper.TryInt(TextHelper.Get(map, ItemFieldNames.Stars));
        dto.Description = TextHelper.CleanText(TextHelper.Get(map, CommonFieldNames.Description));
        if (dto.Description.Contains(WikiSpecialCases.CharacterAscensionMaterial))
            dto.Description = dto.Description.Replace("Character Ascension material.\\n", "");

        // sources: pega TODAS as chaves que começam com "source"
        foreach (KeyValuePair<string, string> kv in map.Where(kv => kv.Key.StartsWith(CommonFieldNames.SourcePrefix, StringComparison.OrdinalIgnoreCase))
                              .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            string val = TextHelper.CleanText(kv.Value);
            if (!string.IsNullOrWhiteSpace(val))
                dto.Sources.Add(val);
        }

        // sanity: precisa ter pelo menos um núcleo (type/description)
        bool hasCore = !string.IsNullOrWhiteSpace(dto.Type) || !string.IsNullOrWhiteSpace(dto.Description);
        return hasCore ? dto : null;
    }
}
