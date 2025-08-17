using System.Text;
using System.Text.RegularExpressions;

namespace Genshin.Wiki.Parser.Helpers;

public static class TextHelper
{
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
        var pattern = @"\{\{\s*" + Regex.Escape(templateName) + @"\b";
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Index : -1;
    }

    // Lê linhas com |key = value (suporta multilinha até o próximo |key)
    public static Dictionary<string, string> ParseTemplateFields(string templateContent)
    {
        // Remove o cabeçalho "Character Infobox"
        var content = Regex.Replace(templateContent, @"^\s*Character\s+Infobox\b", "", RegexOptions.IgnoreCase);

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
                    dict[currentKey] = CleanValue(currentValue.ToString());
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
            dict[currentKey] = CleanValue(currentValue.ToString());
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
        k = Regex.Replace(k, @"<!--.*?-->", "", RegexOptions.Singleline).Trim();

        // alguns dumps têm espaço no final da chave (ex. "weapon ")
        k = k.TrimEnd();

        return k;
    }

    public static string CleanValue(string v)
    {
        if (string.IsNullOrWhiteSpace(v)) return string.Empty;

        var s = v;

        // remove comentários HTML
        s = Regex.Replace(s, @"<!--.*?-->", "", RegexOptions.Singleline);

        // bullets começam com "*"
        s = Regex.Replace(s, @"^\s*\*\s*", "\n*", RegexOptions.Multiline);

        // [http://url Texto] -> "http://url Texto"
        s = Regex.Replace(s, @"\[(https?://[^\s\]]+)\s+([^\]]+)\]", "$1 $2");

        // [[Texto|Exibição]] -> Exibição ; [[Texto]] -> Texto
        s = Regex.Replace(s, @"\[\[([^\|\]]+)\|([^\]]+)\]\]", "$2");
        s = Regex.Replace(s, @"\[\[([^\]]+)\]\]", "$1");

        // Templates COM pipe: {{algo|X}} -> X  (melhor esforço)
        s = Regex.Replace(s, @"\{\{[^{}|]+\|([^{}]+)\}\}", "$1");

        // Templates SEM pipe: {{Cryo}} -> Cryo
        s = Regex.Replace(s, @"\{\{([^{}|]+)\}\}", "$1");

        // <ref>...</ref> -> extrai URL se houver, senão remove conteúdo
        s = Regex.Replace(s, @"<ref[^>]*>(.*?)</ref>", m => ExtractUrlOrText(m.Groups[1].Value) ?? "", RegexOptions.Singleline);

        // remove marcação de itálico/negrito do MediaWiki: '', ''', '''''
        s = Regex.Replace(s, @"'{2,5}", "");

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
        var items = System.Text.RegularExpressions.Regex
            .Matches(raw, @"\*\s*([^\*\r\n]+)")   // captura tudo após cada * até outro * ou quebra
            .Cast<System.Text.RegularExpressions.Match>()
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
            if (Regex.IsMatch(candidate, @"^'{2,}[^']+'{2,}$"))
                continue;
            
            // ignora se só tem nome em negrito
            if (Regex.IsMatch(candidate, @"\b(is a playable|can be obtained)\b", RegexOptions.IgnoreCase))
                continue;

            // preferir frases com " is " ou " was "
            if (Regex.IsMatch(candidate, @"\b(is|was)\b", RegexOptions.IgnoreCase))
                return OneLine(candidate);
        }

        // fallback: primeira linha não-vazia
        return lines.Count > 0 ? OneLine(lines[0]) : null;

        static string OneLine(string s) => Regex.Replace(s, @"\s+", " ").Trim();
    }


    public static string? ExtractUrlOrText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // tenta [http://url ...]
        var m = Regex.Match(raw, @"https?://[^\s\]]+");
        if (m.Success) return m.Value;

        // senão, retorna texto limpo
        return CleanValue(raw);
    }

    public static string? Get(Dictionary<string, string> dict, string key)
        => dict.TryGetValue(key, out var v) ? (string.IsNullOrWhiteSpace(v) ? null : v) : null;

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
}