using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Enemy;

namespace Genshin.Wiki.Parser.Parsers.Enemy;

public static class EnemyParser
{
    public static EnemyDto? TryParse(string? wikitext, string? pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (!wikitext.Contains("{{Enemy Infobox", StringComparison.OrdinalIgnoreCase))
            return null;

        var box = TextHelper.ExtractTemplateBlock(wikitext, "Enemy Infobox");
        if (box is null) return null;

        var fields = TextHelper.ParseTemplateFields(box, "Enemy Infobox");

        // --- tipo / family / group
        var type    = TextHelper.CleanInline(TextHelper.Get(fields, "type"));
        var family  = TextHelper.CleanInline(TextHelper.Get(fields, "family"));
        var group   = TextHelper.CleanInline(TextHelper.Get(fields, "group"));
        var title   = TextHelper.CleanInline(TextHelper.Get(fields, "title"));

        var dto = new EnemyDto
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
        var rx = new Regex(@"\{\{\s*Description\s*\|\s*(.+?)\}\}", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var m = rx.Match(text);
        if (!m.Success) return null;
        return TextHelper.CleanInline(m.Groups[1].Value);
    }
}
