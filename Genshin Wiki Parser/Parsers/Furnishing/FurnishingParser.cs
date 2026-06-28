using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Furnishing;

namespace Genshin.Wiki.Parser.Parsers.Furnishing;

public static class FurnishingParser
{
    public static FurnishingDto? TryParse(string wikiText, string pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikiText)) return null;
        if (!wikiText.Contains($"{WikiSyntax.TemplateOpen}{WikiTemplates.FurnishingInfobox}", StringComparison.OrdinalIgnoreCase)) return null;

        FurnishingDto dto = new FurnishingDto { Title = pageTitle };

        // 1) Infobox
        string infobox = TextHelper.ExtractTemplate(WikiTemplates.FurnishingInfobox, wikiText);
        Dictionary<string, string> map  = TextHelper.ParseTemplateParams(infobox);

        dto.Category      = TextHelper.Get(map, FurnishingFieldNames.Category);
        dto.Subcategory   = TextHelper.Get(map, FurnishingFieldNames.Subcategory);
        dto.Description   = TextHelper.CleanText(TextHelper.Get(map, CommonFieldNames.Description));

        // fontes (source1, source2, ...)
        foreach (KeyValuePair<string, string> kv in map.Where(kv => kv.Key.StartsWith(CommonFieldNames.SourcePrefix, StringComparison.OrdinalIgnoreCase))
                              .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            string val = TextHelper.CleanText(kv.Value);
            if (!string.IsNullOrWhiteSpace(val)) dto.Sources.Add(val);
        }

        // blueprint (onde compra/obtém o diagrama)
        string? blueprint = TextHelper.Get(map, FurnishingFieldNames.Blueprint);
        if (!string.IsNullOrWhiteSpace(blueprint))
            dto.BlueprintSources.Add(TextHelper.CleanText(blueprint));

        return dto;
    }
}
