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
        if (!wikitext.Contains($"{WikiSyntax.TemplateOpen}{WikiTemplates.ArtifactInfobox}", StringComparison.OrdinalIgnoreCase))
            return null;

        string? box = TextHelper.ExtractTemplateBlock(wikitext, WikiTemplates.ArtifactInfobox);
        if (box is null) return null;

        Dictionary<string, string> f = TextHelper.ParseTemplateFields(box, WikiTemplates.ArtifactInfobox);

        // Campos básicos do infobox
        string? set   = TextHelper.CleanInline(TextHelper.Get(f, ArtifactFieldNames.Set));
        string? piece = TextHelper.CleanInline(TextHelper.Get(f, ArtifactFieldNames.Piece));
        ParseImageField(TextHelper.Get(f, CommonFieldNames.Image));

        // Descrições
        string? shortDesc = ExtractDescriptionTemplate(wikitext);
        string? longDesc  = ExtractLongDescription(wikitext);

        ArtifactPieceDto dto = new ArtifactPieceDto
        {
            Title      = TextHelper.CleanInline(TextHelper.Get(f, CommonFieldNames.Title) ?? ""),
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
        string? content = TextHelper.ExtractGalleryContent(imageField);
        if (content is null)
        {
            string? single = TextHelper.CleanInline(imageField);
            if (string.IsNullOrWhiteSpace(single)) return null;
            return new ArtifactImageDto { Primary = single };
        }

        string? primary = null;
        List<string?> extras = new List<string?>();

        foreach (string rawLine in content.Split('\n'))
        {
            string raw = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string[] parts = raw.Split('|', 2);
            string? file  = TextHelper.CleanInline(parts[0]);
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
        string? block = TextHelper.ExtractTemplateBlock(text, WikiTemplates.OtherLanguages);
        if (block is null) return null;

        Dictionary<string, string> f = TextHelper.ParseTemplateFields(block, WikiTemplates.OtherLanguages);
        Dictionary<string, string?> dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string> kv in f)
        {
            // ignora variantes _tl (traduções literais) e _rm (romanizações) e também índices "1_en"
            if (OtherLanguageVariantRegex().IsMatch(kv.Key)) continue;

            // normaliza chaves estilo "1_en" -> "en"
            string key = OtherLanguageIndexPrefixRegex().Replace(kv.Key, "");
            string? val = TextHelper.CleanInline(kv.Value);
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
                dict[key] = val;
        }

        return dict.Count > 0 ? dict : null;
    }

    private static string? CanonicalizePieceKey(string? piece)
    {
        if (string.IsNullOrWhiteSpace(piece)) return null;
        string p = piece.Trim().ToLowerInvariant();
        // cobre nomes completos e abreviações mais comuns
        if (p.Contains(ArtifactPieceNames.FlowerMarker))  return ArtifactPieceNames.Flower;   // Flower of Life
        if (p.Contains(ArtifactPieceNames.PlumeMarker))   return ArtifactPieceNames.Plume;    // Plume of Death
        if (p.Contains(ArtifactPieceNames.SandsMarker))   return ArtifactPieceNames.Sands;    // Sands of Eon
        if (p.Contains(ArtifactPieceNames.GobletMarker))  return ArtifactPieceNames.Goblet;   // Goblet of Eonothem
        if (p.Contains(ArtifactPieceNames.CircletMarker)) return ArtifactPieceNames.Circlet;  // Circlet of Logos
        return null;
    }
}
