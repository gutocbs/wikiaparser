using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Enemy;

namespace Genshin.Wiki.Parser.Parsers.Enemy;

public static class EnemyParser
{
    public static EnemyDto? TryParse(string? wikitext, string? pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (!wikitext.Contains($"{WikiSyntax.TemplateOpen}{WikiTemplates.EnemyInfobox}", StringComparison.OrdinalIgnoreCase))
            return null;

        string? box = TextHelper.ExtractTemplateBlock(wikitext, WikiTemplates.EnemyInfobox);
        if (box is null) return null;

        Dictionary<string, string> fields = TextHelper.ParseTemplateFields(box, WikiTemplates.EnemyInfobox);

        // --- tipo / family / group
        string? type    = TextHelper.CleanInline(TextHelper.Get(fields, CommonFieldNames.Type));
        string? family  = TextHelper.CleanInline(TextHelper.Get(fields, EnemyFieldNames.Family));
        string? group   = TextHelper.CleanInline(TextHelper.Get(fields, CommonFieldNames.Group));
        string? title   = TextHelper.CleanInline(TextHelper.Get(fields, CommonFieldNames.Title));

        EnemyDto dto = new EnemyDto
        {
            Name        = TextHelper.CleanInline(pageTitle ?? ""),
            Title        = title,
            Type         = type,
            Family       = family,
            Group        = group,

            // Templates auxiliares
            ShortDescription    = ExtractFirstDescriptionTemplate(wikitext),
            DescriptionsSection = TextHelper.ExtractSection(wikitext, "Descriptions"),
        };

        // sanity básica
        bool hasCore = !string.IsNullOrWhiteSpace(dto.Title) || dto.Type != null;
        return hasCore ? dto : null;
    }

    // ----------------- Description (primeiro template) -----------------
    private static string? ExtractFirstDescriptionTemplate(string text)
    {
        return TextHelper.ExtractDescriptionTemplate(text);
    }
}
