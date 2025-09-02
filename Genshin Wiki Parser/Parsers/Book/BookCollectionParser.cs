using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Books;

namespace Genshin.Wiki.Parser.Parsers.Book;

public static class BookCollectionParser
{
    public static BookCollectionDto? TryParse(string? wikitext, string? pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (!wikitext.Contains("{{Book Collection Infobox", StringComparison.OrdinalIgnoreCase))
            return null;

        var box = TextHelper.ExtractTemplateBlock(wikitext, "Book Collection Infobox");
        if (box is null) return null;

        var f = TextHelper.ParseTemplateFields(box, "Book Collection Infobox");

        var dto = new BookCollectionDto
        {
            Title          = TextHelper.CleanInline(pageTitle ?? ""),
            Quality        = TextHelper.TryInt(TextHelper.Get(f, "quality")),
            RegionLore     = TextHelper.CleanInline(TextHelper.Get(f, "region_lore")),
            RegionLocation = TextHelper.CleanInline(TextHelper.Get(f, "region_location")),
            VolumeCount    = TextHelper.TryInt(TextHelper.Get(f, "volumes")),
            Author         = TextHelper.CleanInline(TextHelper.Get(f, "author")),
            AcquisitionByVolume = ExtractAcquisitions(f),
            Volumes        = ExtractVolumes(wikitext)
        };

        return dto;
    }

    private static Dictionary<int, List<string>> ExtractAcquisitions(Dictionary<string,string> f)
    {
        var dict = new Dictionary<int, List<string>>();
        var rx = new Regex(@"^vol(\d+)$", RegexOptions.IgnoreCase);

        foreach (var kv in f)
        {
            var m = rx.Match(kv.Key);
            if (!m.Success) continue;
            if (!int.TryParse(m.Groups[1].Value, out var idx)) continue;

            var value = kv.Value ?? "";
            // quebra por <br> / novas linhas
            var split = Regex.Split(value, @"<\s*br\s*/?>|\r?\n", RegexOptions.IgnoreCase);
            var list = new List<string>();
            foreach (var s in split)
            {
                var t = TextHelper.CleanInline(s);
                if (!string.IsNullOrWhiteSpace(t)) list.Add(t);
            }
            if (list.Count > 0)
                dict[idx] = list;
        }
        return dict;
    }

    // ---------- Volumes ----------
    private static List<BookVolumeDto> ExtractVolumes(string text)
    {
        // pega blocos "==Vol. X==" até o próximo "=="
        var rxVol = new Regex(@"^==\s*Vol\.\s*(\d+)\s*==\s*(.+?)(?=^\s*==|\Z)",
                              RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        var list = new List<BookVolumeDto>();
        foreach (Match m in rxVol.Matches(text))
        {
            var idxStr = m.Groups[1].Value;
            if (!int.TryParse(idxStr, out var idx)) continue;

            var body = m.Groups[2].Value;

            // Description no início (se existir)
            string? desc = null;
            var d = Regex.Match(body, @"\{\{\s*Description\s*\|\s*(.+?)\}\}", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (d.Success)
            {
                desc = TextHelper.CleanInline(d.Groups[1].Value);
                // remove a Description do corpo
                body = body.Remove(d.Index, d.Length);
            }

            var cleanText = TextHelper.CleanText(body);

            list.Add(new BookVolumeDto
            {
                Index = idx,
                Description = desc,
                Text = string.IsNullOrWhiteSpace(cleanText) ? null : cleanText
            });
        }

        return list;
    }
}
