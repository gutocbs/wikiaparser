using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Books;

namespace Genshin.Wiki.Parser.Parsers.Book;

public static partial class BookCollectionParser
{
    [GeneratedRegex(@"^vol(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex AcquisitionVolumeKeyRegex();

    [GeneratedRegex(@"<\s*br\s*/?>|\r?\n", RegexOptions.IgnoreCase)]
    private static partial Regex AcquisitionSplitRegex();

    [GeneratedRegex(@"^==\s*Vol\.\s*(\d+)\s*==\s*(.+?)(?=^\s*==|\Z)", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex VolumeSectionRegex();

    public static BookCollectionDto? TryParse(string? wikitext, string? pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (!wikitext.Contains("{{Book Collection Infobox", StringComparison.OrdinalIgnoreCase))
            return null;

        string? box = TextHelper.ExtractTemplateBlock(wikitext, "Book Collection Infobox");
        if (box is null) return null;

        Dictionary<string, string> f = TextHelper.ParseTemplateFields(box, "Book Collection Infobox");

        BookCollectionDto dto = new BookCollectionDto
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
        Dictionary<int, List<string>> dict = new Dictionary<int, List<string>>();
        foreach (KeyValuePair<string, string> kv in f)
        {
            Match m = AcquisitionVolumeKeyRegex().Match(kv.Key);
            if (!m.Success) continue;
            if (!int.TryParse(m.Groups[1].Value, out int idx)) continue;

            string value = kv.Value ?? "";
            // quebra por <br> / novas linhas
            string[] split = AcquisitionSplitRegex().Split(value);
            List<string> list = new List<string>();
            foreach (string s in split)
            {
                string? t = TextHelper.CleanInline(s);
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
        List<BookVolumeDto> list = new List<BookVolumeDto>();
        foreach (Match m in VolumeSectionRegex().Matches(text))
        {
            string idxStr = m.Groups[1].Value;
            if (!int.TryParse(idxStr, out int idx)) continue;

            string body = m.Groups[2].Value;

            // Description no início (se existir)
            string? desc = null;
            Match d = TextHelper.MatchDescriptionTemplate(body);
            if (d.Success)
            {
                desc = TextHelper.CleanInline(d.Groups[1].Value);
                // remove a Description do corpo
                body = body.Remove(d.Index, d.Length);
            }

            string cleanText = TextHelper.CleanText(body);

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
