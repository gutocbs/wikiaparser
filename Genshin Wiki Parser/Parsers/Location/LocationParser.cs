using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Location;

namespace Genshin.Wiki.Parser.Parsers.Location;

public static class LocationParser
{
    // call principal
    public static LocationDto? TryParse(string wikiText, string pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikiText)) return null;
        if (!wikiText.Contains("{{Location Infobox", StringComparison.OrdinalIgnoreCase)) return null;

        var dto = new LocationDto { Title = pageTitle };

        // 1) Infobox
        var infobox = TextHelper.ExtractTemplate("Location Infobox", wikiText);
        if (!string.IsNullOrEmpty(infobox))
        {
            var map = TextHelper.ParseTemplateParams(infobox);

            dto.Type    = TextHelper.Get(map, "type");
            dto.Subtype = TextHelper.Get(map, "type2");
            dto.Region  = TextHelper.Get(map, "region");
            dto.Area    = TextHelper.Get(map, "area");
            dto.Subarea = TextHelper.Get(map, "subarea");
        }

        // 2) Summary do Location Intro (com If Self)
        var intro = TextHelper.ExtractTemplate("Location Intro", wikiText);
        if (!string.IsNullOrEmpty(intro))
        {
            // resolve {{If Self|<página>|A|B}}
            intro = ResolveIfSelf(intro, pageTitle);
            var introMap = TextHelper.ParseTemplateParams(intro);
            var descRaw = TextHelper.Get(introMap, "description");
            if (!string.IsNullOrWhiteSpace(descRaw))
                dto.Summary = TextHelper.CleanText(descRaw);
        }

        // 3) NPCs (seção ==NPCs==)
        foreach (var name in ExtractNpcNames(wikiText))
            dto.Npcs.Add(name);

        // 5) Descriptions (seção ==Descriptions== com {{Description|texto|fonte}})
        foreach (var d in ExtractDescriptionsSection(wikiText))
            dto.Descriptions.Add(d);

        // Sinaliza “inválido” se estiver vazio demais (ajuste como preferir)
        var hasCore =
            !string.IsNullOrWhiteSpace(dto.Type) ||
            !string.IsNullOrWhiteSpace(dto.Region) ||
            !string.IsNullOrWhiteSpace(dto.Summary);
        return hasCore ? dto : null;
    }

    // ---- helpers ----
    private static IEnumerable<string> ExtractNpcNames(string text)
    {
        var m = Regex.Match(text, @"(?ms)^\s*==\s*NPCs\s*==\s*(?<blk>.+?)(?:^\s*==|$)");
        if (!m.Success) yield break;

        var blk = m.Groups["blk"].Value;
        foreach (var line in blk.Split('\n'))
        {
            if (!line.TrimStart().StartsWith("*")) continue;
            var name = TextHelper.CleanText(line.Replace("*", "").Trim());
            if (!string.IsNullOrWhiteSpace(name))
                yield return name;
        }
    }

    private static IEnumerable<LocationDescriptionDto> ExtractDescriptionsSection(string text)
    {
        var m = Regex.Match(text, @"(?ms)^\s*==\s*Descriptions\s*==\s*(?<blk>.+?)(?:^\s*==|$)");
        if (!m.Success) yield break;

        var blk = m.Groups["blk"].Value;
        foreach (Match d in Regex.Matches(blk,
                 @"\{\{\s*Description\s*\|\s*(?<txt>[^|}]+?)(?:\|\s*(?<src>[^|}]+))?\s*\}\}",
                 RegexOptions.IgnoreCase))
        {
            var txt = TextHelper.CleanText(d.Groups["txt"].Value);
            var src = TextHelper.CleanText(d.Groups["src"].Value);
            if (!string.IsNullOrWhiteSpace(txt))
                yield return new LocationDescriptionDto { Text = txt, Source = TextHelper.NullIfEmpty(src) };
        }
    }

    private static string ResolveIfSelf(string raw, string pageTitle)
    {
        return Regex.Replace(raw,
            @"\{\{\s*If\s*Self\s*\|\s*([^|\}]+)\|\s*([^|\}]+)\|\s*([^|\}]+)\s*\}\}",
            m =>
            {
                var page = TextHelper.CleanText(m.Groups[1].Value);
                var ifYes = TextHelper.CleanText(m.Groups[2].Value);
                var ifNo  = TextHelper.CleanText(m.Groups[3].Value);
                return page.Equals(pageTitle, StringComparison.OrdinalIgnoreCase) ? ifYes : ifNo;
            },
            RegexOptions.IgnoreCase);
    }
}
