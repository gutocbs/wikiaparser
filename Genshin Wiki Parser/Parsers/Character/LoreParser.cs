using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Character;

namespace Genshin.Wiki.Parser.Parsers.Character;

public static class LoreParser
{
    public static LoreDto? TryParse(string? wikitext, string? pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (string.IsNullOrWhiteSpace(pageTitle)) return null;
        if (!pageTitle.EndsWith("/Lore", StringComparison.OrdinalIgnoreCase)) return null;

        var dto = new LoreDto
        {
            Character = pageTitle[..pageTitle.IndexOf("/Lore", StringComparison.OrdinalIgnoreCase)]
        };

        // 1) Quotes (todas) + primeira como SummaryQuote
        var quotes = ExtractQuotes(wikitext);
        dto.Quotes = quotes.Count > 0 ? quotes : null;
        dto.SummaryQuote = dto.Quotes?.FirstOrDefault()?.Text;

        // 2) Official Introduction (template)
        dto.OfficialIntroduction = ExtractOfficialIntroduction(wikitext);

        // 3) Seções comuns
        dto.Personality = TextHelper.ExtractSection(wikitext, "Personality");
        dto.Appearance  = TextHelper.ExtractSection(wikitext, "Appearance");

        // 4) Character Stories (template com N campos numerados)
        dto.CharacterStories = ExtractCharacterStories(wikitext);

        // Se tudo nulo, retorna null para não emitir um objeto vazio
        if (TextHelper.IsEmpty(dto)) return null;
        return dto;
    }

    // ---------- QUOTES ----------
    private static List<QuoteDto> ExtractQuotes(string text)
    {
        var list = new List<QuoteDto>();

        // Puxa blocos {{Quote|...}} com captura "preguiçosa" – nos dumps isso costuma ser simples
        var rx = new Regex(@"\{\{\s*Quote\s*\|(.*?)\}\}", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        foreach (Match m in rx.Matches(text))
        {
            var content = m.Groups[1].Value; // pode ser "texto|fonte" ou só "texto"

            // split pela primeira barra vertical fora de tags <ref> simples
            var parts = SplitFirstPipe(content);

            var q = new QuoteDto
            {
                Text   = TextHelper.CleanText(parts.Item1),
                Source = TextHelper.CleanText(parts.Item2)
            };
            if (!string.IsNullOrWhiteSpace(q.Text))
                list.Add(q);
        }

        return list;
    }

    // divide A|B em (A,B), se houver '|', senão (A,null)
    private static (string, string?) SplitFirstPipe(string s)
    {
        var idx = s.IndexOf('|');
        if (idx < 0) return (s, null);
        return (s[..idx], s[(idx + 1)..]);
    }

    // ---------- OFFICIAL INTRO ----------
    private static OfficialIntroDto? ExtractOfficialIntroduction(string text)
    {
        var block = TextHelper.ExtractTemplateBlock(text, "Official Introduction");
        if (block == null) return null;

        var fields = TextHelper.ParseTemplateFields(block, "Official Introduction");
        var dto = new OfficialIntroDto
        {
            Title     = TextHelper.Get(fields, "title"),
            Link      = TextHelper.Get(fields, "link"),
            Character = TextHelper.Get(fields, "character")
        };

        return TextHelper.IsEmpty(dto) ? null : dto;
    }

    // ---------- CHARACTER STORIES ----------
    private static List<CharacterStoryDto>? ExtractCharacterStories(string text)
    {
        var block = TextHelper.ExtractTemplateBlock(text, "Character Story");
        if (block == null) return null;

        var fields = TextHelper.ParseTemplateFields(block, "Character Story");

        // Descobrir quantos índices existem (titleN, textN, ...)
        var maxN = 0;
        foreach (var k in fields.Keys)
        {
            var m = Regex.Match(k, @"^(?:title|text|friendship|mention)(\d+)$", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n) && n > maxN) maxN = n;
        }

        var list = new List<CharacterStoryDto>();
        for (int n = 1; n <= maxN; n++)
        {
            var title = TextHelper.Get(fields, $"title{n}");
            var textN = TextHelper.CleanText(TextHelper.Get(fields, $"text{n}"));
            var frStr = TextHelper.Get(fields, $"friendship{n}");
            int? friendship = null;
            if (int.TryParse(frStr, out var f)) friendship = f;

            var mentionsStr = TextHelper.Get(fields, $"mention{n}");
            List<string>? mentions = null;
            if (!string.IsNullOrWhiteSpace(mentionsStr))
            {
                mentions = mentionsStr
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => TextHelper.CleanInline(s))
                    .Where(s => s.Length > 0)
                    .ToList();
                if (mentions.Count == 0) mentions = null;
            }

            // ignora histórias sem texto
            if (string.IsNullOrWhiteSpace(textN)) continue;

            list.Add(new CharacterStoryDto
            {
                Title = TextHelper.CleanInline(title),
                Text = textN,
                Friendship = friendship,
                Mentions = mentions
            });
        }

        return list.Count > 0 ? list : null;
    }

    
}