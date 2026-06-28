using System.Text;
using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.RegexPatterns;

namespace Genshin.Wiki.Parser.Helpers;

public static partial class TextHelper
{
    private static readonly Dictionary<string, string> Replacements = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Cataclysm|destruction", "Cataclysm" }
    };
    
    // Extrai bloco de template com chaves balanceadas, ex.: {{Character Infobox ... }}
    public static string? ExtractTemplateBlock(string text, string templateName)
    {
        var idx = IndexOfTemplateStart(text, templateName);
        if (idx < 0) return null;

        int i = idx;
        int depth = 0;
        var sb = new StringBuilder();

        while (i < text.Length)
        {
            if (i + 1 < text.Length && text[i] == '{' && text[i + 1] == '{')
            {
                depth++;
                sb.Append("{{");
                i += 2;
                continue;
            }
            if (i + 1 < text.Length && text[i] == '}' && text[i + 1] == '}')
            {
                depth--;
                sb.Append("}}");
                i += 2;
                if (depth == 0) break;
                continue;
            }
            sb.Append(text[i]);
            i++;
        }

        var block = sb.ToString();
        // Tira a casca "{{" + "}}" externa
        if (block.StartsWith("{{", StringComparison.Ordinal) &&
            block.EndsWith("}}", StringComparison.Ordinal) &&
            block.Length >= 4)
        {
            return block[2..^2].Trim(); // sem as chaves externas
        }
        return block.Trim();
    }

    public static int IndexOfTemplateStart(string text, string templateName)
    {
        // procura "{{Character Infobox" ignorando case e espaços após {{
        var m = DynamicPatterns.GetTemplateStartRegex(templateName).Match(text);
        return m.Success ? m.Index : -1;
    }

    // Lê linhas com |key = value (suporta multilinha até o próximo |key)
    public static Dictionary<string, string> ParseTemplateFields(string templateContent)
    {
        // Remove o cabeçalho "Character Infobox"
        var content = CharacterPatterns.CharacterInfoboxHeader().Replace(templateContent, "");

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentKey = null;
        var currentValue = new StringBuilder();

        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            // ignora a galeria inteira (é volumosa e irrelevante para o DTO)
            if (line.TrimStart().StartsWith("|image", StringComparison.OrdinalIgnoreCase))
            {
                // consome linhas até fechar </gallery> (ou até próximo |key)
                ConsumeImageBlock(reader, ref line);
                continue;
            }

            if (line.StartsWith("|"))
            {
                // finalize o campo anterior
                if (currentKey != null)
                {
                    dict[currentKey] = CleanValue(currentValue.ToString()) ?? string.Empty;
                }

                // novo campo
                var idx = line.IndexOf('=');
                if (idx > 1)
                {
                    currentKey = NormalizeKey(line[1..idx].Trim());
                    currentValue.Clear();
                    currentValue.Append(line[(idx + 1)..].Trim());
                }
                else
                {
                    // linha com '|' mas sem '=', considera inválida; reseta
                    currentKey = null;
                    currentValue.Clear();
                }
            }
            else
            {
                // continuação de valor multiline
                if (currentKey != null)
                {
                    currentValue.Append('\n');
                    currentValue.Append(line.TrimEnd());
                }
            }
        }

        // último campo
        if (currentKey != null)
        {
            dict[currentKey] = CleanValue(currentValue.ToString()) ?? string.Empty;
        }

        return dict;
    }

    public static void ConsumeImageBlock(StringReader reader, ref string line)
    {
        // Já estamos numa linha que começa com |image
        // Se houver <gallery>, consome até </gallery>
        var sb = new StringBuilder();
        sb.AppendLine(line);

        bool inGallery = line.Contains("<gallery>", StringComparison.OrdinalIgnoreCase);
        while (true)
        {
            var l = reader.ReadLine();
            if (l == null) break;
            sb.AppendLine(l);
            if (inGallery && l.IndexOf("</gallery>", StringComparison.OrdinalIgnoreCase) >= 0)
                break;
            // também paramos se encontrar claramente o começo de outro campo
            if (!inGallery && l.StartsWith("|")) break;
        }
        // no retorno normal, nada a fazer — optamos por não salvar imagem no DTO
    }

    // Normaliza a chave: remove comentários de fim, espaços e trailing colon
    public static string NormalizeKey(string raw)
    {
        var k = raw.Trim();

        // remove comentários no final da chave (raro, mas aparece)
        k = HtmlPatterns.HtmlComment().Replace(k, "").Trim();

        // alguns dumps têm espaço no final da chave (ex. "weapon ")
        k = k.TrimEnd();

        return k;
    }

    public static string? CleanValue(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return string.Empty;

        var s = v;

        // remove comentários HTML
        s = HtmlPatterns.HtmlComment().Replace(s, "");

        // bullets começam com "*"
        s = TextCleanupPatterns.LeadingBullet().Replace(s, "\n*");

        // [http://url Texto] -> "http://url Texto"
        s = LinkPatterns.ExternalLink().Replace(s, "$1 $2");

        // [[Texto|Exibição]] -> Exibição ; [[Texto]] -> Texto
        s = LinkPatterns.WikiPipeLink().Replace(s, "$2");
        s = LinkPatterns.WikiLink().Replace(s, "$1");

        // Templates COM pipe: {{algo|X}} -> X  (melhor esforço)
        s = TemplatePatterns.TemplateWithPipe().Replace(s, "$1");

        // Templates SEM pipe: {{Cryo}} -> Cryo
        s = TemplatePatterns.TemplateWithoutPipe().Replace(s, "$1");

        // <ref>...</ref> -> extrai URL se houver, senão remove conteúdo
        s = HtmlPatterns.RefTagCapture().Replace(s, m => ExtractUrlOrText(m.Groups[1].Value) ?? "");

        // remove marcação de itálico/negrito do MediaWiki: '', ''', '''''
        s = TextCleanupPatterns.WikiQuoteMarkup().Replace(s, "");

        // entidades simples
        s = s.Replace("&mdash;", "—");

        // normaliza espaços
        s = s.Trim();

        return s;
    }

    public static string NormalizeObtain(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw ?? string.Empty;

        // 1) tenta capturar bullets do tipo "* algo", mesmo que tudo esteja em uma linha
        var items = Regex
            .Matches(raw, @"\*\s*([^\*\r\n]+)")   // captura tudo após cada * até outro * ou quebra
            .Cast<Match>()
            .Select(m => m.Groups[1].Value.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        if (items.Count > 0)
            return string.Join(", ", items);

        // 2) fallback: se não achou bullets, tenta dividir por quebras/; / | e limpar asteriscos residuais
        var parts = raw
            .Split(new[] { '\r', '\n', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().TrimStart('*').Trim())
            .Where(s => s.Length > 0);

        return string.Join(", ", parts);
    }



    public static string? ExtractDescription(string fullText, string infoboxBlock)
    {
        var startIdx = IndexOfTemplateStart(fullText, "Character Infobox");
        if (startIdx < 0) return null;

        string block = "{{" + infoboxBlock + "}}";
        int after = startIdx + block.Length + 1;

        if (after >= fullText.Length) return null;

        var remainder = fullText[after..];

        // pega até o próximo "==", que marca uma seção
        var endSection = remainder.IndexOf("\n==", StringComparison.Ordinal);
        var search = endSection >= 0 ? remainder[..endSection] : remainder;

        // quebra em linhas, remove vazias
        var lines = search.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(CleanValue)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        foreach (var l in lines)
        {
            var candidate = l.Trim();

            // ignora se só tem nome em negrito
            if (TextCleanupPatterns.QuotedOnly().IsMatch(candidate))
                continue;
            
            // ignora se só tem nome em negrito
            if (CharacterPatterns.PlayableOrObtained().IsMatch(candidate))
                continue;

            // preferir frases com " is " ou " was "
            if (CharacterPatterns.IsOrWas().IsMatch(candidate))
                return OneLine(candidate);
        }

        // fallback: primeira linha não-vazia
        return lines.Count > 0 ? OneLine(lines[0]) : null;

        static string OneLine(string s) => TextCleanupPatterns.Whitespace().Replace(s, " ").Trim();
    }


    public static string? ExtractUrlOrText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // tenta [http://url ...]
        var m = LinkPatterns.Url().Match(raw);
        if (m.Success) return m.Value;

        // senão, retorna texto limpo
        return CleanValue(raw);
    }

    public static string? Get(Dictionary<string, string> dict, string key)
        => dict.TryGetValue(key, out var v) ? string.IsNullOrWhiteSpace(v) ? null : v : null;

    public static bool IsEmpty(object? o)
    {
        if (o == null) return true;
        foreach (var p in o.GetType().GetProperties())
        {
            var v = p.GetValue(o);
            if (v is string s && !string.IsNullOrWhiteSpace(s)) return false;
            if (v is not string && v != null) return false;
        }
        return true;
    }
    
    public static Dictionary<string, string> ParseTemplateFields(string templateContent, string headerName)
    {
        // remove cabeçalho "TemplateName"
        var content = DynamicPatterns.GetTemplateHeaderRegex(headerName).Replace(templateContent, "");

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentKey = null;
        var currentValue = new StringBuilder();

        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.TrimStart().StartsWith("|"))
            {
                // fecha o campo anterior
                if (currentKey != null)
                    dict[currentKey] = CleanFieldValue(currentValue.ToString());
                
                var idx = line.IndexOf('=');
                if (idx > 1)
                {
                    currentKey = NormalizeKey(line[1..idx]);
                    currentValue.Clear();
                    currentValue.Append(line[(idx + 1)..].Trim());
                }
                else
                {
                    currentKey = null;
                    currentValue.Clear();
                }
            }
            else
            {
                if (currentKey != null)
                {
                    currentValue.Append('\n');
                    currentValue.Append(line.TrimEnd());
                }
            }
        }

        if (currentKey != null)
            dict[currentKey] = CleanFieldValue(currentValue.ToString()) ?? string.Empty;

        return dict;
    }

    public static string? CleanFieldValue(string v) => CleanText(v);

    public static string CleanText(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return string.Empty;
        if (v.AsSpan().IndexOfAny("<[{\'&\n") < 0)
            return v.Trim();

        var s = v;

        // comentários HTML
        s = HtmlPatterns.HtmlComment().Replace(s, "");

        // <ref>...</ref> → remove (ou poderia extrair URLs, se quiser)
        s = HtmlPatterns.RefTag().Replace(s, "");

        // <p> → quebra; outras tags → remove
        s = HtmlPatterns.ParagraphTag().Replace(s, "\n\n");
        s = HtmlPatterns.BreakTag().Replace(s, "\n");
        s = HtmlPatterns.HtmlTag().Replace(s, "");

        // [http url texto] → "texto" (ou "texto (url)" se preferir)
        s = LinkPatterns.ExternalLink().Replace(s, "$2");

        // links wiki
        s = LinkPatterns.WikiPipeLink().Replace(s, "$2"); // [[A|B]]→B
        s = LinkPatterns.WikiLink().Replace(s, "$1");     // [[A]]→A
        s = LinkPatterns.CategoryLink().Replace(s, "$1");

        // templates COM pipe: {{x|Y}} → Y (melhor esforço)
        s = TemplatePatterns.TemplateWithPipe().Replace(s, "$1");

        // templates SEM pipe: {{Cryo}} → Cryo ; {{sic|[[Akasha]]}} já caiu na regra com pipe acima
        s = TemplatePatterns.TemplateWithoutPipe().Replace(s, "$1");

        // negrito/itálico de wiki
        s = TextCleanupPatterns.WikiQuoteMarkup().Replace(s, "");

        // entidades comuns
        s = s.Replace("&mdash;", "—");

        // normaliza quebras múltiplas
        s = TextCleanupPatterns.TrailingHorizontalWhitespace().Replace(s, "\n");
        s = TextCleanupPatterns.MultipleNewLines().Replace(s, "\n\n");

        return s.Trim();
    }

    public static string? ReplaceText(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        if (Replacements.Any(x => line.Contains(x.Key)))
        {
            KeyValuePair<string, string> replacement = Replacements.FirstOrDefault(x => line.Contains(x.Key));
            line = line.Replace(replacement.Key, replacement.Value);
        }
        return line;
    }

    public static string? CleanInline(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        ReplaceText(line);
        
        // versão “inline”: não preserva parágrafos
        var t = CleanText(line);
        t = TextCleanupPatterns.Whitespace().Replace(t, " ").Trim();
        return t;
    }
    
    public static string GetBaseKey(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        var t = title.Trim();

        // remove namespace se existir (User:, Template:, etc.)
        var colon = t.IndexOf(':');
        if (colon >= 0) t = t[(colon + 1)..];

        // pega só antes da primeira subpágina (/Lore, /Voicelines, etc.)
        var slash = t.IndexOf('/');
        if (slash >= 0) t = t[..slash];

        return t.Trim();
    }

    public static string BaseCharacterFromTitle(string title)
    {
        var t = title.Trim();
        var colon = t.IndexOf(':'); if (colon >= 0) t = t[(colon + 1)..];
        var slash = t.IndexOf('/'); if (slash >= 0) t = t[..slash];
        return t.Trim();
    }

    public static string? StripParens(string? s)
    {
        var t = s.Trim();
        if (t.StartsWith("(") && t.EndsWith(")") && t.Length > 1)
            return t[1..^1].Trim();
        return t;
    }
    
    public static string? ExtractSection(string fullText, string heading)
    {
        // procura "== Heading ==" (varia número de "="; usamos \s* para tolerância)
        var m = DynamicPatterns.GetSectionHeadingRegex(heading).Match(fullText);
        if (!m.Success) return null;

        int start = m.Index + m.Length;
        // até o próximo heading de mesmo nível (ou qualquer == ... ==)
        var next = TextCleanupPatterns.AnyHeading().Match(fullText[start..]);
        string section = next.Success ? fullText.Substring(start, next.Index) : fullText[start..];

        // limpa wiki/HTML preservando parágrafos
        var cleaned = CleanText(section);
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    public static bool ContainsIgnoreCase(string? hay, string needle)
        => hay?.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    public static bool IsUrl(string value) => value.Contains("http", StringComparison.OrdinalIgnoreCase);

    public static List<string>? ToList(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        // divide por vírgulas / quebras / <br> já devem ter sido normalizadas por CleanInline
        var list = v.Split(new[] { ',', ';', '/', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => CleanInline(s))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return list.Count > 0 ? list : null;
    }

    public static string? ExtractDescriptionTemplate(string text)
    {
        var blk = ExtractTemplateBlock(text, "Description");
        if (blk is null) return null;
        var m = DescriptionPatterns.DescriptionTemplateBody().Match(blk);
        return m.Success ? CleanInline(m.Groups[1].Value) : null;
    }

    public static Match MatchDescriptionTemplate(string text) => DescriptionPatterns.DescriptionTemplate().Match(text);

    public static string? ExtractGalleryContent(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var m = HtmlPatterns.GalleryRegex().Match(text);
        return m.Success ? m.Groups[1].Value : null;
    }

    public static string? ExtractDescriptionSection(string text)
    {
        var m = DescriptionPatterns.DescriptionSection().Match(text);
        if (!m.Success) return null;
        var body = CleanText(m.Groups[1].Value.Trim());
        return string.IsNullOrWhiteSpace(body) ? null : body;
    }

    public static string? ExtractChangeHistoryVersion(string text)
    {
        var m = CharacterPatterns.ChangeHistoryVersion().Match(text);
        return m.Success ? CleanInline(m.Groups[1].Value) : null;
    }

    public static MatchCollection MatchQuoteTemplates(string text) => DialoguePatterns.QuoteTemplate().Matches(text);

    public static MatchCollection MatchDialogueBlocks(string text) => DialoguePatterns.DialogueBlock().Matches(text);

    public static string? ExtractDialogueBlockContent(string section)
    {
        var start = DialoguePatterns.DialogueStart().Match(section);
        var end = DialoguePatterns.DialogueEnd().Match(section);
        if (!start.Success || !end.Success || end.Index <= start.Index) return null;
        return section.Substring(start.Index + start.Length, end.Index - (start.Index + start.Length)).Trim();
    }

    public static string RemoveAudioTemplates(string text) => DialoguePatterns.AudioTemplate().Replace(text, "");

    public static List<string>? ExtractAudioFiles(string? text, out string? withoutAudio)
    {
        var files = new List<string?>();
        withoutAudio = text;

        if (string.IsNullOrEmpty(text)) return null;

        withoutAudio = DialoguePatterns.AudioTemplate().Replace(text, m =>
        {
            var file = CleanInline(m.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(file)) files.Add(file);
            return "";
        });

        return files.Count > 0 ? files : null;
    }

    public static bool StartsWithDialogueIcon(string text) => DialoguePatterns.DialogueIconStart().IsMatch(text);

    public static bool ContainsDialogueIcon(string text) => DialoguePatterns.DialogueIcon().IsMatch(text);

    public static string RemoveDialogueIcons(string text) => DialoguePatterns.DialogueIcon().Replace(text, "");

    public static decimal? ToDecimal(string? s)
        => decimal.TryParse((s ?? "").Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : null;

    public static int? ToInt(string? s)
        => int.TryParse((s ?? "").Trim(), out var n) ? n : null;

    public static decimal? ToPercent(Dictionary<string,string> f, string key)
    {
        if (!f.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Trim().TrimEnd('%');
        return ToDecimal(raw);
    }

    public static string? ExtractFileFromCell(string cell)
    {
        // procura [[File:...]]
        var m = TableCellPatterns.FileCell().Match(cell);
        if (m.Success) return m.Groups[1].Value.Trim();
        return CleanCell(cell);
    }

    public static string CleanCell(string s)
    {
        var t = s;

        // <br> vira " - " pra juntar partes
        t = TableCellPatterns.CellBreak().Replace(t, " - ");

        // remove <small>...</small>
        t = HtmlPatterns.SmallTag().Replace(t, "$1");

        // remove itálico/bold wiki ''...''
        t = TextCleanupPatterns.WikiApostropheMarkup().Replace(t, "");

        // tira refs
        t = HtmlPatterns.RefTagOrSelfClosing().Replace(t, "");

        // desmarca link simples [[A|B]] / [[A]]
        t = TableCellPatterns.CellWikiPipeLink().Replace(t, "$2");
        t = TableCellPatterns.CellWikiLink().Replace(t, "$1");

        // html entities comuns
        t = t.Replace("&mdash;", "—").Replace("&ndash;", "–");

        return CleanInline(t);
    }
    
    public static int? TryInt(string? s) => int.TryParse((s ?? "").Trim(), out var n) ? n : null;

    public static string NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public static string StripNowiki(string s) => string.IsNullOrEmpty(s) ? s : HtmlPatterns.NoWikiTag().Replace(s, "");

    public static string ExtractTemplate(string name, string text)
    {
        // simples/non-greedy; funciona bem para esses infoboxes
        var m = DynamicPatterns.GetTemplateRegex(name).Match(text);
        return m.Success ? m.Groups["body"].Value : null;
    }

    public static Dictionary<string, string> ParseTemplateParams(string body)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(body)) return dict;

        // | key = value   (para; até próximo | ou fim de template)
        foreach (Match m in TemplatePatterns.TemplateParam().Matches(body))
        {
            var k = m.Groups["k"].Value.Trim();
            var v = m.Groups["v"].Value.Trim();
            dict[k] = v;
        }
        return dict;
    }
    
    public static IEnumerable<string> ExtractTemplates(string name, string text)
    {
        foreach (Match m in DynamicPatterns.GetTemplateRegex(name).Matches(text))
            yield return m.Groups["body"].Value;
    }
}
