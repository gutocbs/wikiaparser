using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Character;

namespace Genshin.Wiki.Parser.Parsers.Character;

public static partial class LoreParser
{
    [GeneratedRegex(@"^(?:title|text|friendship|mention)(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex CharacterStoryFieldRegex();

    public static LoreDto? TryParse(string? wikitext, string? pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (string.IsNullOrWhiteSpace(pageTitle)) return null;
        if (!pageTitle.EndsWith(WikiPageMarkers.LoreSuffix, StringComparison.OrdinalIgnoreCase)) return null;

        LoreDto dto = new LoreDto
        {
            Character = pageTitle[..pageTitle.IndexOf(WikiPageMarkers.LoreSuffix, StringComparison.OrdinalIgnoreCase)]
        };

        // 1) Quotes (todas) + primeira como SummaryQuote
        List<QuoteDto> quotes = ExtractQuotes(wikitext);
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
        List<QuoteDto> list = new List<QuoteDto>();

        // Puxa blocos {{Quote|...}} com captura "preguiçosa" – nos dumps isso costuma ser simples
        foreach (Match m in TextHelper.MatchQuoteTemplates(text))
        {
            string content = m.Groups[1].Value; // pode ser "texto|fonte" ou só "texto"

            // split pela primeira barra vertical fora de tags <ref> simples
            (string, string?) parts = SplitFirstPipe(content);

            QuoteDto q = new QuoteDto
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
        int idx = s.IndexOf('|');
        if (idx < 0) return (s, null);
        return (s[..idx], s[(idx + 1)..]);
    }

    // ---------- OFFICIAL INTRO ----------
    private static OfficialIntroDto? ExtractOfficialIntroduction(string text)
    {
        string? block = TextHelper.ExtractTemplateBlock(text, WikiTemplates.OfficialIntroduction);
        if (block == null) return null;

        Dictionary<string, string> fields = TextHelper.ParseTemplateFields(block, WikiTemplates.OfficialIntroduction);
        OfficialIntroDto dto = new OfficialIntroDto
        {
            Title     = TextHelper.Get(fields, CommonFieldNames.Title),
            Link      = TextHelper.Get(fields, LoreFieldNames.Link),
            Character = TextHelper.Get(fields, LoreFieldNames.Character)
        };

        return TextHelper.IsEmpty(dto) ? null : dto;
    }

    // ---------- CHARACTER STORIES ----------
    private static List<CharacterStoryDto>? ExtractCharacterStories(string text)
    {
        string? block = TextHelper.ExtractTemplateBlock(text, WikiTemplates.CharacterStory);
        if (block == null) return null;

        Dictionary<string, string> fields = TextHelper.ParseTemplateFields(block, WikiTemplates.CharacterStory);

        // Descobrir quantos índices existem (titleN, textN, ...)
        int maxN = 0;
        foreach (string k in fields.Keys)
        {
            Match m = CharacterStoryFieldRegex().Match(k);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int n) && n > maxN) maxN = n;
        }

        List<CharacterStoryDto> list = new List<CharacterStoryDto>();
        for (int n = 1; n <= maxN; n++)
        {
            string? title = TextHelper.Get(fields, $"{LoreFieldNames.TitlePrefix}{n}");
            string textN = TextHelper.CleanText(TextHelper.Get(fields, $"{LoreFieldNames.TextPrefix}{n}"));
            string? frStr = TextHelper.Get(fields, $"{LoreFieldNames.FriendshipPrefix}{n}");
            int? friendship = null;
            if (int.TryParse(frStr, out int f)) friendship = f;

            string? mentionsStr = TextHelper.Get(fields, $"{LoreFieldNames.MentionPrefix}{n}");
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
                Text = textN.Replace("\n", " "),
                Friendship = friendship,
                Mentions = mentions
            });
        }

        return list.Count > 0 ? list : null;
    }

    
}
