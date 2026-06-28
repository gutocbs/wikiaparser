using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Faction;

namespace Genshin.Wiki.Parser.Parsers.Faction;

public static partial class FactionParser
{
    private static readonly ConcurrentDictionary<string, Regex> SectionRegexCache = new(StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"^===\s*Members\s*===\s*(.+?)(?=^===|\Z)", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex MembersSectionRegex();

    [GeneratedRegex(@"\{\|(.+?)\|\}", RegexOptions.Singleline)]
    private static partial Regex WikiTableRegex();

    [GeneratedRegex(@"^\s*\|-\s*$", RegexOptions.Multiline)]
    private static partial Regex TableRowSeparatorRegex();

    [GeneratedRegex(@"^\s*!", RegexOptions.Multiline)]
    private static partial Regex TableHeaderRowRegex();

    [GeneratedRegex(@"\[\[\s*([^[\]|]+)(?:\|([^[\]]+))?\s*\]\]")]
    private static partial Regex WikiLinkRegex();

    [GeneratedRegex(@"^====\s*Former Members\s*====\s*(.+?)(?=^===|\Z)", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex FormerMembersSectionRegex();

    [GeneratedRegex(@"^(.*?)\s*\((.*?)\)\s*$")]
    private static partial Regex ParenthesizedNoteRegex();

    [GeneratedRegex(@"^===\s*Employees\s*===\s*(.+?)(?=^===|\Z)", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex EmployeesSectionRegex();

    [GeneratedRegex(@"^====\s*(.+?)\s*====\s*(.+?)(?=^====|\Z)", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex EmployeeSubsectionRegex();

    [GeneratedRegex(@"\{\{\s*column\s*\|\s*\d+\s*\|\s*(.+?)\}\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ColumnTemplateRegex();

    [GeneratedRegex(@"^(.*?)\s+[—-]\s+(.*)$")]
    private static partial Regex DashSeparatedRoleRegex();

    private static Regex GetSectionRegex(string sectionName)
        => SectionRegexCache.GetOrAdd(sectionName, static name =>
            new Regex(@"^==\s*" + Regex.Escape(name) + @"\s*==\s*(.+?)(?=^\s*==|\Z)",
                RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled));

    public static FactionDto? TryParse(string? wikitext, string? pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (!wikitext.Contains($"{WikiSyntax.TemplateOpen}{WikiTemplates.FactionInfobox}", StringComparison.OrdinalIgnoreCase))
            return null;

        string? box = TextHelper.ExtractTemplateBlock(wikitext, WikiTemplates.FactionInfobox);
        if (box is null) return null;

        Dictionary<string, string> fields = TextHelper.ParseTemplateFields(box, WikiTemplates.FactionInfobox);

        if(wikitext.Contains(WikiSpecialCases.KhaenriahAbyssOrderSkipText))
            Console.WriteLine("sdadd");
        FactionDto dto = new FactionDto
        {
            Title  = TextHelper.CleanInline(pageTitle ?? ""),
            Base   = TextHelper.CleanInline(TextHelper.Get(fields, FactionFieldNames.Base)),
            Region = TextHelper.CleanInline(TextHelper.Get(fields, CommonFieldNames.Region)),

            Quotes            = ExtractQuotes(wikitext),
            History           = ExtractSection(wikitext, "History"),
            Responsibilities  = ExtractSection(wikitext, "Responsibilities"),
            Associates        = ExtractSection(wikitext, "Associates"),
            Members           = ParseMembersTable(wikitext),
            FormerMembers     = ParseFormerMembers(wikitext),
            EmployeesByGroup  = ParseEmployees(wikitext)
        };

        return dto;
    }

    // ---------- Infobox: image pode vir com <gallery> ou simples ----------
    private static string? ExtractInfoboxImage(string? field)
    {
        if (string.IsNullOrWhiteSpace(field)) return null;
        // tenta dentro de <gallery>
        string? content = TextHelper.ExtractGalleryContent(field);
        if (content is not null)
        {
            foreach (string line in content.Split('\n'))
            {
                string raw = line.Trim();
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string[] parts = raw.Split('|', 2);
                string? file = TextHelper.CleanInline(parts[0]);
                if (!string.IsNullOrWhiteSpace(file)) return file;
            }
        }
        // senão, retorna "cru" limpo
        return TextHelper.CleanInline(field);
    }

    // ---------- Quotes: {{Quote|texto|falante|contexto(opc)}} múltiplos ----------
    private static List<FactionQuoteDto>? ExtractQuotes(string text)
    {
        List<FactionQuoteDto> list = new List<FactionQuoteDto>();
        foreach (Match m in TextHelper.MatchQuoteTemplates(text))
        {
            string blob = m.Groups[1].Value;
            List<string> parts = SplitTemplateParams(TextHelper.ReplaceText(blob) ?? blob);
            FactionQuoteDto q = new FactionQuoteDto();
            q.Text = parts.Count > 0 ? TextHelper.CleanInline(parts[0])?.Replace("quote = ","") : null;
            if (parts.Count > 1)
            {
                string? tempText = TextHelper.CleanInline(parts[1])?.Replace("speaker = ","");
                q.Speaker = tempText is null ? null : tempText.Contains("/Lore#Character Story") ? tempText.Split(",").FirstOrDefault() : tempText;
            }
            q.Context = parts.Count > 2 ? TextHelper.CleanInline(parts[2])?.Replace("source = ","").Replace("]]","").Replace("[[","") : null;
            if (!string.IsNullOrWhiteSpace(q.Text)) list.Add(q);
        }
        return list.Count > 0 ? list : null;
    }

    // Helper simples pra quebrar params de template em nível 1
    private static List<string> SplitTemplateParams(string s)
    {
        // divide por | respeitando não aninhar chaves profundamente (versão simples)
        List<string> parts = new List<string>();
        int depth = 0; int start = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (i+1 < s.Length && s[i] == '{' && s[i+1] == '{') { depth++; i++; continue; }
            if (i+1 < s.Length && s[i] == '}' && s[i+1] == '}') { depth--; i++; continue; }
            if (s[i] == '|' && depth == 0)
            {
                parts.Add(s.Substring(start, i - start));
                start = i + 1;
            }
        }
        parts.Add(s.Substring(start));
        return parts;
    }

    // ---------- Sections ----------
    private static string? ExtractSection(string text, string sectionName)
    {
        Match m = GetSectionRegex(sectionName).Match(text);
        if (!m.Success) return null;
        string body = TextHelper.CleanText(m.Groups[1].Value);
        return string.IsNullOrWhiteSpace(body) ? null : body;
    }

    // ---------- Members (wikitable) ----------
    private static List<FactionMemberDto>? ParseMembersTable(string text)
    {
        // pega a seção "===Members===" e o primeiro {|
        Match secM = MembersSectionRegex().Match(text);
        if (!secM.Success) return null;

        string sec = secM.Groups[1].Value;
        Match tbl = WikiTableRegex().Match(sec);
        if (!tbl.Success) return null;

        List<string> rows = TableRowSeparatorRegex().Split(tbl.Groups[1].Value.Trim()).ToList();

        List<FactionMemberDto> members = new List<FactionMemberDto>();
        foreach (string row in rows)
        {
            // pula cabeçalho (linhas começando com "!")
            if (TableHeaderRowRegex().IsMatch(row)) continue;

            // coleta células que começam com "|"
            List<string> cells = new List<string>();
            foreach (string line in row.Split('\n'))
            {
                string ln = line.TrimStart();
                if (ln.StartsWith("|"))
                {
                    cells.Add(ln.Substring(1).Trim());
                }
            }
            if (cells.Count < 5) continue; // esperamos 5 colunas (Icon, Name, Title, Star, Responsibility)

            (string? name, string? link) = ExtractLink(cells[1]);
            string title   = TextHelper.CleanCell(cells[2]);
            string star    = TextHelper.CleanCell(cells[3]); // mantém "Dubhe (Alpha Ursae Majoris)" já limpo
            string resp    = TextHelper.CleanCell(cells[4]);

            members.Add(new FactionMemberDto
            {
                Name     = name,
                NameLink = link,
                Title    = title,
                Star     = star,
                Responsibility = resp
            });
        }

        return members.Count > 0 ? members : null;
    }

    private static (string? display, string? link) ExtractLink(string cell)
    {
        // [[Target|Display]] ou [[Display]]
        Match m = WikiLinkRegex().Match(cell);
        if (m.Success)
        {
            string target = m.Groups[1].Value.Trim();
            string disp   = m.Groups[2].Success ? m.Groups[2].Value.Trim() : target;
            return (TextHelper.CleanInline(disp), TextHelper.CleanInline(target));
        }
        return (TextHelper.CleanCell(cell), null);
    }

    // ---------- Former Members (lista com '*') ----------
    private static List<FactionFormerMemberDto>? ParseFormerMembers(string text)
    {
        Match sec = FormerMembersSectionRegex().Match(text);
        if (!sec.Success) return null;

        string body = sec.Groups[1].Value;
        List<FactionFormerMemberDto> list = new List<FactionFormerMemberDto>();

        foreach (string line in body.Split('\n'))
        {
            string ln = line.Trim();
            if (!ln.StartsWith("*")) continue;

            // exemplo: * [[Yun Hui]]<ref>...</ref><ref group="Note">...</ref> (title unknown)
            string clean = TextHelper.CleanCell(ln.TrimStart('*').Trim());
            // tenta separar " — " / " - " / parênteses
            string? name = clean;
            string? note = null;

            Match mParen = ParenthesizedNoteRegex().Match(clean);
            if (mParen.Success)
            {
                name = mParen.Groups[1].Value.Trim();
                note = mParen.Groups[2].Value.Trim();
            }

            list.Add(new FactionFormerMemberDto
            {
                Name = name,
                Note = note
            });
        }

        return list.Count > 0 ? list : null;
    }

    // ---------- Employees agrupados por subheading (ex.: Wangshu Inn) ----------
    private static Dictionary<string, List<FactionEmployeeDto>>? ParseEmployees(string text)
    {
        Match sec = EmployeesSectionRegex().Match(text);
        if (!sec.Success) return null;

        string body = sec.Groups[1].Value;
        Dictionary<string, List<FactionEmployeeDto>> dict = new Dictionary<string, List<FactionEmployeeDto>>(StringComparer.OrdinalIgnoreCase);

        // sub-seções "====Name====" (ex.: Wangshu Inn) + o bloco até o próximo ==== ou fim
        MatchCollection matches = EmployeeSubsectionRegex().Matches(body);

        if (matches.Count == 0)
        {
            // Sem subheading: parse bullets diretos no body (às vezes vem dentro de {{column|2| ... }})
            List<FactionEmployeeDto> list = ParseEmployeeBullets(body);
            if (list.Count > 0) dict["Employees"] = list;
        }
        else
        {
            foreach (Match m in matches)
            {
                string? groupName = TextHelper.CleanInline(m.Groups[1].Value);
                string groupBody = m.Groups[2].Value;
                List<FactionEmployeeDto> list = ParseEmployeeBullets(groupBody);
                if (list.Count > 0)
                    dict[groupName] = list;
            }
        }

        return dict.Count > 0 ? dict : null;
    }

    private static List<FactionEmployeeDto> ParseEmployeeBullets(string body)
    {
        // Se vier em {{column|2| ... }}, extraímos o conteúdo
        Match col = ColumnTemplateRegex().Match(body);
        if (col.Success) body = col.Groups[1].Value;

        List<FactionEmployeeDto> list = new List<FactionEmployeeDto>();
        foreach (string raw in body.Split('\n'))
        {
            string line = raw.Trim();
            if (!line.StartsWith("*")) continue;

            string clean = TextHelper.CleanCell(line.TrimStart('*').Trim());
            // normalmente "Nome — Papel" (— ou -)
            string? name = clean;
            string? role = null;

            Match mDash = DashSeparatedRoleRegex().Match(clean);
            if (mDash.Success)
            {
                name = mDash.Groups[1].Value.Trim();
                role = mDash.Groups[2].Value.Trim();
            }

            list.Add(new FactionEmployeeDto { Name = name, Role = role });
        }
        return list;
    }
}
