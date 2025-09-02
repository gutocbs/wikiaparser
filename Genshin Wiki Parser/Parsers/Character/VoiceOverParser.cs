using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Character;

namespace Genshin.Wiki.Parser.Parsers.Character;

public static class VoiceOverParser
{
    /// <summary>
    /// Tenta parsear páginas "X/Voice-Overs".
    /// languageCode é opcional; se informado substitui {language} nos nomes de arquivo.
    /// </summary>
    public static VoiceOversDto? TryParse(string? wikitext, string? pageTitle, string? languageCode = null)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (string.IsNullOrWhiteSpace(pageTitle)) return null;
        if (!pageTitle.EndsWith("/Voice-Overs", StringComparison.OrdinalIgnoreCase)) return null;

        var character = BaseTitle(pageTitle);

        var dto = new VoiceOversDto { Character = character };

        // --- VO/Story ---
        var storyBlock = TextHelper.ExtractTemplateBlock(wikitext, "VO/Story");
        if (storyBlock is not null)
        {
            dto.Story = ParseVoStory(storyBlock, character, languageCode);
            if (dto.Story != null && dto.Story.Count == 0) dto.Story = null;
        }

        // --- Combat VO ---
        var combatBlock =TextHelper. ExtractTemplateBlock(wikitext, "Combat VO");
        if (combatBlock is not null)
        {
            dto.Combat = ParseCombatVo(combatBlock, character, languageCode);
            if (dto.Combat is { Count: 0 }) dto.Combat = null;
            dto.Combat = dto.Combat?.Where(kvp => kvp.Value.Any(c => !string.IsNullOrWhiteSpace(c.Text)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Where(c => !string.IsNullOrWhiteSpace(c.Text)).ToList());
        }

        return TextHelper.IsEmpty(dto) ? null : dto;
    }

    private static List<VoStoryLineDto>? ParseVoStory(string templateContent, string character, string? languageCode)
    {
        var fields = TextHelper.ParseTemplateFields(templateContent, "VO/Story");

        // regex: vo_XX_YY_suffix
        var rx = new Regex(@"^vo_(\d{2})_(\d{2})_(title|tx|file|friendship|mention|quest|ascension)$",
                           RegexOptions.IgnoreCase);

        var byBase = new Dictionary<string, VoStoryLineDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in fields)
        {
            var key = kv.Key; var val = kv.Value;
            var m = rx.Match(key);
            if (!m.Success) continue;

            var g = m.Groups[1].Value;   // grupo XX
            var i = m.Groups[2].Value;   // índice YY
            var suf = m.Groups[3].Value.ToLowerInvariant();
            var baseKey = $"vo_{g}_{i}";

            if (!byBase.TryGetValue(baseKey, out var line))
            {
                line = new VoStoryLineDto { Key = baseKey };
                byBase[baseKey] = line;
            }

            switch (suf)
            {
                case "title":
                    line.Title = TextHelper.CleanInline(val)?.Replace("{character}", "Player").Replace("{name}", "Player");
                    break;
                case "tx":
                    line.Text = TextHelper.CleanText(val).Replace("{character}", "Player").Replace("{name}", "Player");
                    break;
                case "friendship":
                    if (int.TryParse(val.Trim(), out var f)) line.Friendship = f;
                    break;
                case "ascension":
                    if (int.TryParse(val.Trim(), out var a)) line.Ascension = a;
                    break;
                case "mention":
                    line.Mentions = SplitList(val);
                    break;
                case "quest":
                    line.Quest = TextHelper.CleanInline(val);
                    break;
            }
        }

        // Ordena por (grupo, índice)
        var ordered = byBase
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => kv.Value)
            .ToList();

        return ordered;
    }

    // ------------- Combat VO -------------

    private static Dictionary<string, List<VoCueDto>>? ParseCombatVo(string templateContent, string character, string? languageCode)
    {
        var fields = TextHelper.ParseTemplateFields(templateContent, "Combat VO");

        // ex.: "skill_1_tx", "burst_3_tx", "sprint-s_1_file", "hit-h_3_file"
        var rx = new Regex(@"^([a-z\-]+)_(\d+?)_(tx|file)$", RegexOptions.IgnoreCase);

        var map = new Dictionary<string, Dictionary<int, VoCueDto>>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in fields)
        {
            var key = kv.Key; var val = kv.Value;
            var m = rx.Match(key);
            if (!m.Success) continue;

            var cat = m.Groups[1].Value;                 // categoria
            var idxStr = m.Groups[2].Value;              // "1", "2"...
            var suf = m.Groups[3].Value.ToLowerInvariant(); // "tx" ou "file"
            if (!int.TryParse(idxStr, out var idx)) continue;

            if (!map.TryGetValue(cat, out var byIndex))
            {
                byIndex = new Dictionary<int, VoCueDto>();
                map[cat] = byIndex;
            }
            if (!byIndex.TryGetValue(idx, out var cue))
            {
                cue = new VoCueDto { Index = idx };
                byIndex[idx] = cue;
            }

            if (suf == "tx")
                cue.Text = TextHelper.CleanText(val);
            else
                cue.Files = SplitFiles(val, character, languageCode);
        }

        if (map.Count == 0) return null;

        // materializa em Dictionary<string, List<VoCueDto>> ordenando por Index
        var result = new Dictionary<string, List<VoCueDto>>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in map.Keys)
        {
            result[cat] = map[cat].Values.OrderBy(c => c.Index).ToList();
        }
        return result;
    }

    // ===================== HELPERS =====================

    private static List<string>? SplitFiles(string? raw, string character, string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var items = raw
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Select(s =>
            {
                // substitui {character}/{language} se desejar
                var r = s.Replace("{character}", character);
                if (!string.IsNullOrEmpty(languageCode))
                    r = r.Replace("{language}", languageCode);
                return r;
            })
            .ToList();
        return items.Count > 0 ? items : null;
    }

    private static List<string>? SplitList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var items = raw
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => TextHelper.CleanInline(s))
            .Where(s => s.Length > 0)
            .ToList();
        return items.Count > 0 ? items : null;
    }

    private static string BaseTitle(string full)
    {
        var t = full;
        var colon = t.IndexOf(':'); if (colon >= 0) t = t[(colon + 1)..];
        var slash = t.IndexOf('/'); if (slash >= 0) t = t[..slash];
        return t.Trim();
    }
}
