using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Location;

namespace Genshin.Wiki.Parser.Parsers.Location;

public static partial class LocationParser
{
    [GeneratedRegex(@"(?ms)^\s*==\s*NPCs\s*==\s*(?<blk>.+?)(?:^\s*==|$)")]
    private static partial Regex NpcsSectionRegex();

    [GeneratedRegex(@"(?ms)^\s*==\s*Descriptions\s*==\s*(?<blk>.+?)(?:^\s*==|$)")]
    private static partial Regex DescriptionsSectionRegex();

    [GeneratedRegex(@"\{\{\s*Description\s*\|\s*(?<txt>[^|}]+?)(?:\|\s*(?<src>[^|}]+))?\s*\}\}", RegexOptions.IgnoreCase)]
    private static partial Regex LocationDescriptionTemplateRegex();

    [GeneratedRegex(@"\{\{\s*If\s*Self\s*\|\s*([^|\}]+)\|\s*([^|\}]+)\|\s*([^|\}]+)\s*\}\}", RegexOptions.IgnoreCase)]
    private static partial Regex IfSelfTemplateRegex();

    // call principal
    public static LocationDto? TryParse(string wikiText, string pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikiText)) return null;
        if (!wikiText.Contains("{{Location Infobox", StringComparison.OrdinalIgnoreCase)) return null;

        LocationDto dto = new LocationDto { Title = pageTitle };

        // 1) Infobox
        string infobox = TextHelper.ExtractTemplate("Location Infobox", wikiText);
        if (!string.IsNullOrEmpty(infobox))
        {
            Dictionary<string, string> map = TextHelper.ParseTemplateParams(infobox);

            dto.Type    = TextHelper.Get(map, "type");
            dto.Subtype = TextHelper.Get(map, "type2");
            dto.Region  = TextHelper.Get(map, "region");
            dto.Area    = TextHelper.Get(map, "area");
            dto.Subarea = TextHelper.Get(map, "subarea");
        }

        // 2) Summary do Location Intro (com If Self)
        string intro = TextHelper.ExtractTemplate("Location Intro", wikiText);
        if (!string.IsNullOrEmpty(intro))
        {
            // resolve {{If Self|<página>|A|B}}
            intro = ResolveIfSelf(intro, pageTitle);
            Dictionary<string, string> introMap = TextHelper.ParseTemplateParams(intro);
            string? descRaw = TextHelper.Get(introMap, "description");
            if (!string.IsNullOrWhiteSpace(descRaw))
                dto.Summary = TextHelper.CleanText(descRaw);
        }

        // 3) NPCs (seção ==NPCs==)
        foreach (string name in ExtractNpcNames(wikiText))
            dto.Npcs.Add(name);

        // 5) Descriptions (seção ==Descriptions== com {{Description|texto|fonte}})
        foreach (LocationDescriptionDto d in ExtractDescriptionsSection(wikiText))
            dto.Descriptions.Add(d);

        // Sinaliza “inválido” se estiver vazio demais (ajuste como preferir)
        bool hasCore =
            !string.IsNullOrWhiteSpace(dto.Type) ||
            !string.IsNullOrWhiteSpace(dto.Region) ||
            !string.IsNullOrWhiteSpace(dto.Summary);
        return hasCore ? dto : null;
    }

    // ---- helpers ----
    private static IEnumerable<string> ExtractNpcNames(string text)
    {
        Match m = NpcsSectionRegex().Match(text);
        if (!m.Success) yield break;

        string blk = m.Groups["blk"].Value;
        foreach (string line in blk.Split('\n'))
        {
            if (!line.TrimStart().StartsWith("*")) continue;
            string name = TextHelper.CleanText(line.Replace("*", "").Trim());
            if (!string.IsNullOrWhiteSpace(name))
                yield return name;
        }
    }

    private static IEnumerable<LocationDescriptionDto> ExtractDescriptionsSection(string text)
    {
        Match m = DescriptionsSectionRegex().Match(text);
        if (!m.Success) yield break;

        string blk = m.Groups["blk"].Value;
        foreach (Match d in LocationDescriptionTemplateRegex().Matches(blk))
        {
            string txt = TextHelper.CleanText(d.Groups["txt"].Value);
            string src = TextHelper.CleanText(d.Groups["src"].Value);
            if (!string.IsNullOrWhiteSpace(txt))
                yield return new LocationDescriptionDto { Text = txt, Source = TextHelper.NullIfEmpty(src) };
        }
    }

    private static string ResolveIfSelf(string raw, string pageTitle)
    {
        return IfSelfTemplateRegex().Replace(raw,
            m =>
            {
                string page = TextHelper.CleanText(m.Groups[1].Value);
                string ifYes = TextHelper.CleanText(m.Groups[2].Value);
                string ifNo  = TextHelper.CleanText(m.Groups[3].Value);
                return page.Equals(pageTitle, StringComparison.OrdinalIgnoreCase) ? ifYes : ifNo;
            });
    }
}
