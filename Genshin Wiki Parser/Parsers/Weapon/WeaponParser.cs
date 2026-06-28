using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Weapon;

namespace Genshin.Wiki.Parser.Parsers.Weapon;

public static partial class WeaponParser
{
    [GeneratedRegex(@"\((var\d+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex PassiveEffectVariableRegex();

    [GeneratedRegex(@"^eff_rank([1-5])_var(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex PassiveVarKeyRegex();

    public static WeaponDto? TryParse(string? wikitext)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (!wikitext.Contains("{{Weapon Infobox", StringComparison.OrdinalIgnoreCase))
            return null;

        string? box = TextHelper.ExtractTemplateBlock(wikitext, "Weapon Infobox");
        if (box is null) return null;

        Dictionary<string, string> f = TextHelper.ParseTemplateFields(box, "Weapon Infobox");

        int? ToInt(string? s) => int.TryParse(s?.Trim(), out int n) ? n : null;

        string? StripSoftHyphens(string? s)
        {
            if (!string.IsNullOrWhiteSpace(s))
                return s.Replace("&shy;", "", StringComparison.OrdinalIgnoreCase);
            return s;
        }

        // --- imagens do <gallery> dentro do campo image ---
        ParseWeaponImages(TextHelper.Get(f, "image"));

        // --- passive vars: eff_rankN_varM ---
        Dictionary<string, string[]>? passiveVars = ParsePassiveVars(f);

        // --- passive attributes: eff_att1..N ---
        List<string?>? attrs = f.Where(kv => kv.Key.StartsWith("eff_att", StringComparison.OrdinalIgnoreCase))
                     .Select(kv => TextHelper.CleanInline(kv.Value))
                     .Where(v => !string.IsNullOrWhiteSpace(v))
                     .ToList();
        if (attrs.Count == 0) attrs = null;

        // --- efeito com placeholders {varX} no lugar de (varX) ---
        string effectTemplate = TextHelper.CleanInline(TextHelper.Get(f, "effect")) ?? "";
        effectTemplate = PassiveEffectVariableRegex().Replace(effectTemplate, "{$1}");

        WeaponDto dto = new WeaponDto
        {
            Title              = StripSoftHyphens(TextHelper.CleanInline(TextHelper.Get(f, "title") ?? "")),
            Id                 = ToInt(TextHelper.Get(f, "id")),
            Type               = TextHelper.CleanInline(TextHelper.Get(f, "type")),
            Series             = TextHelper.CleanInline(TextHelper.Get(f, "series")),
            Quality            = ToInt(TextHelper.Get(f, "quality")),
            BaseAtk            = ToInt(TextHelper.Get(f, "base_atk")),
            SecondaryStatType  = TextHelper.CleanInline(TextHelper.Get(f, "2nd_stat_type")),
            SecondaryStat      = TextHelper.CleanInline(TextHelper.Get(f, "2nd_stat")),
            Obtain             = TextHelper.CleanInline(TextHelper.Get(f, "obtain")),
            PassiveName        = TextHelper.CleanInline(TextHelper.Get(f, "passive")),
            PassiveEffectTemplate = string.IsNullOrWhiteSpace(effectTemplate) ? null : effectTemplate,
            PassiveVars        = passiveVars,
            PassiveAttributes  = attrs,
            ShortDescription   = ExtractDescriptionTemplate(wikitext),
            LongDescription    = ExtractLongDescription(wikitext),
            Ascension          = ParseAscension(wikitext),
        };

        // sanity: se quase nada, retorna null
        if (dto.Id is null && dto.Type is null && dto.BaseAtk is null && dto.PassiveName is null)
            return null;

        return dto;
    }

    // ---------- helpers específicos deste parser ----------

    private static WeaponImagesDto? ParseWeaponImages(string? imageField)
    {
        if (string.IsNullOrWhiteSpace(imageField)) return null;

        string content = imageField;
        content = TextHelper.ExtractGalleryContent(imageField) ?? content;

        string? baseImg = null, asc2 = null;
        List<string?> extras = new List<string?>();

        foreach (string rawLine in content.Split('\n'))
        {
            string raw = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string[] parts = raw.Split('|', 2);
            string? file  = TextHelper.CleanInline(parts[0]);
            string? label = parts.Length > 1 ? TextHelper.CleanInline(parts[1]) : null;

            if (string.IsNullOrWhiteSpace(file)) continue;
            string lab = (label ?? "").ToLowerInvariant();

            if (lab.Contains("base") && baseImg is null) baseImg = file;
            else if (lab.Contains("2nd") && asc2 is null) asc2 = file;
            else extras.Add(string.IsNullOrWhiteSpace(label) ? file : $"{file} ({label})");
        }

        if (baseImg is null && asc2 is null && extras.Count == 0) return null;
        return new WeaponImagesDto { Base = baseImg, Ascension2 = asc2, Extras = extras.Count > 0 ? extras : null };
    }

    private static Dictionary<string, string[]>? ParsePassiveVars(Dictionary<string, string?> fields)
    {
        // mapeia "var1","var2",... -> array [rank1..rank5]
        Dictionary<string, string?[]> dict = new Dictionary<string, string?[]>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string?> kv in fields)
        {
            Match m = PassiveVarKeyRegex().Match(kv.Key);
            if (!m.Success) continue;

            int rank = int.Parse(m.Groups[1].Value);        // 1..5
            string varName = "var" + m.Groups[2].Value;     // var1, var2, ...

            if (!dict.TryGetValue(varName, out string?[]? arr))
            {
                arr = new string[5];
                dict[varName] = arr;
            }

            arr[rank - 1] = TextHelper.CleanInline(kv.Value);
        }

        // limpa variáveis que não têm pelo menos 1 valor
        List<string> emptyKeys = dict.Where(k => k.Value.All(string.IsNullOrWhiteSpace))
                            .Select(k => k.Key)
                            .ToList();
        foreach (string k in emptyKeys) dict.Remove(k);

        return dict.Count > 0 ? dict : null;
    }

    private static WeaponAscensionDto? ParseAscension(string text)
    {
        string? blk = TextHelper.ExtractTemplateBlock(text, "Weapon Ascensions and Stats");
        if (blk is null) return null;
        Dictionary<string, string> f = TextHelper.ParseTemplateFields(blk, "Weapon Ascensions and Stats");

        List<string> Pack(string prefix, int max)
            => Enumerable.Range(1, max)
                         .Select(i => TextHelper.CleanInline(TextHelper.Get(f, $"{prefix}{i}")))
                         .Where(s => !string.IsNullOrWhiteSpace(s))
                         .ToList();

        List<string> ascend = Pack("ascendMat", 4);
        List<string> boss   = Pack("bossMat",   3);
        List<string> common = Pack("commonMat", 3);

        if (ascend.Count == 0 && boss.Count == 0 && common.Count == 0) return null;

        return new WeaponAscensionDto
        {
            AscendMats = ascend.Count > 0 ? ascend : null,
            BossMats   = boss.Count   > 0 ? boss   : null,
            CommonMats = common.Count > 0 ? common : null
        };
    }

    private static string? ExtractDescriptionTemplate(string text)
    {
        return TextHelper.ExtractDescriptionTemplate(text);
    }

    private static string? ExtractLongDescription(string text)
    {
        return TextHelper.ExtractDescriptionSection(text);
    }

    private static string? ExtractChangeHistoryVersion(string text)
    {
        return TextHelper.ExtractChangeHistoryVersion(text);
    }
}
