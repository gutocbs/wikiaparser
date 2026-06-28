using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Artifacts;

namespace Genshin.Wiki.Parser.Parsers.Artifact;

public static partial class ArtifactParser
{
    [GeneratedRegex(@"_(tl|rm)$", RegexOptions.IgnoreCase)]
    private static partial Regex OtherLanguageVariantRegex();

    [GeneratedRegex(@"^\d+_", RegexOptions.IgnoreCase)]
    private static partial Regex OtherLanguageIndexPrefixRegex();

    public static ArtifactPieceDto? TryParse(string? wikitext)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (!wikitext.Contains("{{Artifact Infobox", StringComparison.OrdinalIgnoreCase))
            return null;

        var box = TextHelper.ExtractTemplateBlock(wikitext, "Artifact Infobox");
        if (box is null) return null;

        var f = TextHelper.ParseTemplateFields(box, "Artifact Infobox");

        // Campos básicos do infobox
        var set   = TextHelper.CleanInline(TextHelper.Get(f, "set"));
        var piece = TextHelper.CleanInline(TextHelper.Get(f, "piece"));
        ParseImageField(TextHelper.Get(f, "image"));

        // Descrições
        var shortDesc = ExtractDescriptionTemplate(wikitext);
        var longDesc  = ExtractLongDescription(wikitext);

        var dto = new ArtifactPieceDto
        {
            Title      = TextHelper.CleanInline(TextHelper.Get(f, "title") ?? ""),
            Set        = string.IsNullOrWhiteSpace(set) ? null : set,
            Piece      = string.IsNullOrWhiteSpace(piece) ? null : piece,
            ShortDescription = shortDesc,
            LongDescription  = longDesc
        };

        // sanity: precisa pelo menos Set e Piece
        if (dto.Set is null && dto.Piece is null && dto.Title is null)
            return null;

        return dto;
    }

    // ----------------- helpers específicos -----------------

    private static ArtifactImageDto? ParseImageField(string? imageField)
    {
        if (string.IsNullOrWhiteSpace(imageField)) return null;

        // pode vir como "<gallery> ... </gallery>" ou só "File.png"
        var content = TextHelper.ExtractGalleryContent(imageField);
        if (content is null)
        {
            var single = TextHelper.CleanInline(imageField);
            if (string.IsNullOrWhiteSpace(single)) return null;
            return new ArtifactImageDto { Primary = single };
        }

        string? primary = null;
        var extras = new List<string?>();

        foreach (var rawLine in content.Split('\n'))
        {
            var raw = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var parts = raw.Split('|', 2);
            var file  = TextHelper.CleanInline(parts[0]);
            if (string.IsNullOrWhiteSpace(file)) continue;

            if (primary is null) primary = file;
            else extras.Add(file);
        }

        if (primary is null && extras.Count == 0) return null;
        return new ArtifactImageDto
        {
            Primary = primary,
            Extras  = extras.Count > 0 ? extras : null
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

    private static Dictionary<string,string>? ExtractOtherLanguages(string text)
    {
        var block = TextHelper.ExtractTemplateBlock(text, "Other Languages");
        if (block is null) return null;

        var f = TextHelper.ParseTemplateFields(block, "Other Languages");
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in f)
        {
            // ignora variantes _tl (traduções literais) e _rm (romanizações) e também índices "1_en"
            if (OtherLanguageVariantRegex().IsMatch(kv.Key)) continue;

            // normaliza chaves estilo "1_en" -> "en"
            var key = OtherLanguageIndexPrefixRegex().Replace(kv.Key, "");
            var val = TextHelper.CleanInline(kv.Value);
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
                dict[key] = val;
        }

        return dict.Count > 0 ? dict : null;
    }

    private static string? CanonicalizePieceKey(string? piece)
    {
        if (string.IsNullOrWhiteSpace(piece)) return null;
        var p = piece.Trim().ToLowerInvariant();
        // cobre nomes completos e abreviações mais comuns
        if (p.Contains("flower"))  return "Flower";   // Flower of Life
        if (p.Contains("plume"))   return "Plume";    // Plume of Death
        if (p.Contains("sands"))   return "Sands";    // Sands of Eon
        if (p.Contains("goblet"))  return "Goblet";   // Goblet of Eonothem
        if (p.Contains("circlet")) return "Circlet";  // Circlet of Logos
        return null;
    }
    // - CleanInline(string? s)  // versão “single-line” do CleanText
}
