using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Character;

namespace Genshin.Wiki.Parser.Parsers.Character;

public static class NamecardParser
{
    public static NamecardDto? TryParse(string? wikitext, string? pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikitext) || string.IsNullOrWhiteSpace(pageTitle))
            return null;

        // precisa ter "Item Infobox"
        var box = TextHelper.ExtractTemplateBlock(wikitext, "Item Infobox");
        if (box is null) return null;

        var fields = TextHelper.ParseTemplateFields(box, "Item Infobox");

        // checa se é Namecard
        var type  = TextHelper.Get(fields, "type");
        var group = TextHelper.Get(fields, "group");
        if (!TextHelper.ContainsIgnoreCase(type, "Namecard") && !TextHelper.ContainsIgnoreCase(group, "Namecard"))
            return null;

        // título e personagem
        var (character, fullTitle) = SplitNamecardTitle(pageTitle);

        // campos simples
        int? TryInt(string? s) => int.TryParse(s?.Trim(), out var n) ? n : null;

        var dto = new NamecardDto
        {
            Character   = character,
            Title       = fullTitle.Split(":")
                                   .Skip(1)
                                   .FirstOrDefault()?.Trim() ?? fullTitle, // título sem namespace,
            Id          = TryInt(TextHelper.Get(fields, "id")),
            Description = TextHelper.CleanInline(TextHelper.Get(fields, "description")),
            Sources     = ExtractSources(fields),
        };

        // se praticamente nada foi encontrado, retorna null
        if (dto.Id is null && dto.Description is null)
            return null;

        return dto;
    }

    // --- title "Faruzan: Sealed Secret" -> ("Faruzan", "Faruzan: Sealed Secret")
    private static (string character, string title) SplitNamecardTitle(string title)
    {
        // remove namespace se houver (ns=0, mas por via das dúvidas)
        var t = title.Trim();
        var colonNs = t.IndexOf(':'); 
        if (colonNs >= 0 && !Regex.IsMatch(t, @"^\s*[^:]+:\s")) // namespace no começo? mantemos o resto
            t = t[(colonNs + 1)..].Trim();

        // captura antes do primeiro ':' como personagem
        var m = Regex.Match(t, @"^\s*([^:]+?)\s*:\s*(.+)$");
        if (m.Success)
            return (m.Groups[1].Value.Trim(), t);

        // fallback: sem dois-pontos, devolve tudo como título e personagem vazio
        return ("", t);
    }
    
    // --- Collect source1..sourceN
    private static List<string>? ExtractSources(Dictionary<string, string?> fields)
    {
        var list = new List<string?>();
        foreach (var kv in fields)
        {
            if (!kv.Key.StartsWith("source", StringComparison.OrdinalIgnoreCase)) continue;
            var clean = TextHelper.CleanInline(kv.Value);
            if (!string.IsNullOrWhiteSpace(clean)) list.Add(clean);
        }
        return list.Count > 0 ? list : null;
    }
}
