using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Item;

namespace Genshin.Wiki.Parser.Parsers.Item;

public static class ItemParser
{
    public static ItemDto? TryParse(string wikiText, string pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikiText)) return null;
        if (!wikiText.Contains("{{Item Infobox", StringComparison.OrdinalIgnoreCase)) return null;

        var dto = new ItemDto { Title = pageTitle };

        // 1) Infobox
        var infobox = TextHelper.ExtractTemplate("Item Infobox", wikiText);
        var map  = TextHelper.ParseTemplateParams(infobox);

        dto.Id          = TextHelper.TryInt(TextHelper.Get(map, "id"));
        dto.Type        = TextHelper.Get(map, "type");
        dto.Group       = TextHelper.Get(map, "group");
        dto.Quality     = TextHelper.TryInt(TextHelper.Get(map, "quality")) ?? TextHelper.TryInt(TextHelper.Get(map, "rarity")) ?? TextHelper.TryInt(TextHelper.Get(map, "stars"));
        dto.Description = TextHelper.CleanText(TextHelper.Get(map, "description"));
        if (dto.Description.Contains("Character Ascension material"))
            dto.Description = dto.Description.Replace("Character Ascension material.\\n", "");

        // sources: pega TODAS as chaves que começam com "source"
        foreach (var kv in map.Where(kv => kv.Key.StartsWith("source", StringComparison.OrdinalIgnoreCase))
                              .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            var val = TextHelper.CleanText(kv.Value);
            if (!string.IsNullOrWhiteSpace(val))
                dto.Sources.Add(val);
        }

        // sanity: precisa ter pelo menos um núcleo (type/description)
        var hasCore = !string.IsNullOrWhiteSpace(dto.Type) || !string.IsNullOrWhiteSpace(dto.Description);
        return hasCore ? dto : null;
    }
}
