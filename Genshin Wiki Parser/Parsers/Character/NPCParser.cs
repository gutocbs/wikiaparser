using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Character;
using Genshin.Wiki.Parser.Models.Quest;

namespace Genshin.Wiki.Parser.Parsers.Character;

public static partial class NpcParser
{
    [GeneratedRegex(@"^'''\s*(?<sp>[^:'\n]+?)\s*:\s*'''\s*(?<tx>.*)$")]
    private static partial Regex DialogueSpeakerRegex();

    private static readonly HashSet<string> FamilyKeys = new(StringComparer.OrdinalIgnoreCase)
        { "father", "sibling", "mother", "spouse", "child", "relative" };
    
    public static NpcDto? TryParse(string wikitext, string? title)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (!wikitext.Contains($"{WikiSyntax.TemplateOpen}{WikiTemplates.CharacterInfobox}", StringComparison.OrdinalIgnoreCase))
            return null;

        string? box = TextHelper.ExtractTemplateBlock(wikitext, WikiTemplates.CharacterInfobox);
        if (box is null) return null;

        Dictionary<string, string> fields = TextHelper.ParseTemplateFields(box, WikiTemplates.CharacterInfobox);

        string? type = TextHelper.CleanInline(TextHelper.Get(fields, CommonFieldNames.Type));
        // Se o tipo explicitamente diz NPC, seguimos; caso não tenha type, ainda dá pra aceitar (muitos NPCs têm).
        if (!string.IsNullOrWhiteSpace(type) && type.IndexOf(WikiPageMarkers.NpcType, StringComparison.OrdinalIgnoreCase) < 0)
        {
            // tem infobox de personagem, mas não é NPC → provavelmente Playable/Enemy/etc.
            return null;
        }

        NpcDto dto = new NpcDto
        {
            Name        = TextHelper.CleanInline(title),
            RealName    = TextHelper.CleanInline(TextHelper.Get(fields, CharacterFieldNames.RealName)),
            Type        = type,
            Element     = TextHelper.CleanInline(TextHelper.Get(fields, CharacterFieldNames.Element)),
            Region      = TextHelper.CleanInline(TextHelper.Get(fields, CommonFieldNames.Region)),
            Locations   = TextHelper.ToList(TextHelper.Get(fields, CharacterFieldNames.Location)),
            Affiliations= TextHelper.ToList(TextHelper.Get(fields, CharacterFieldNames.Affiliation)),
            Title       = TextHelper.CleanInline(TextHelper.Get(fields, CommonFieldNames.Title)),
            Deceased    = TextHelper.CleanInline(TextHelper.Get(fields, CharacterFieldNames.Deceased)),
            Family = ExtractFamily(fields),
            ShortDescription = TextHelper.ExtractDescriptionTemplate(wikitext),
            Profile     = TextHelper.ExtractSection(wikitext, "Profile"),
            Appearance  = TextHelper.ExtractSection(wikitext, "Appearance"),
        };

        // 3) Descrição da Quest (template)
        dto.Dialogues = ExtractDialog(wikitext);

        // Se praticamente nada foi preenchido, evita poluir:
        bool hasCore =
            !string.IsNullOrWhiteSpace(dto.Name) ||
            !string.IsNullOrWhiteSpace(dto.Type);
        return hasCore ? dto : null;
    }
    
    private static List<DetailDto>? ExtractFamily(Dictionary<string, string> fields)
    {
        Dictionary<string, string> familyFields = fields
            .Where(kvp => FamilyKeys.Any(t => kvp.Key.Contains(t, StringComparison.OrdinalIgnoreCase)) && !kvp.Key.Contains("Ref"))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, fields.Comparer);
        
        List<DetailDto>? familyDto = new List<DetailDto>();
        if(familyFields.Count > 0)
        {
            foreach (KeyValuePair<string, string> family in familyFields)
            {
                string familyKey = family.Key;
                string? familyValue = TextHelper.Get(fields, family.Key);
                if (!string.IsNullOrWhiteSpace(familyKey) && !string.IsNullOrWhiteSpace(familyValue))
                {
                    familyDto.Add(new DetailDto
                    {
                        Title = TextHelper.CleanInline(familyKey),
                        Note = TextHelper.CleanInline(familyValue)
                    });
                }
            }
        }
        else
            return null;

        return familyDto;
    }
    public static List<DialogueSection> ExtractDialog(string wikiText)
    {
        List<DialogueSection> result = new List<DialogueSection>();
        if (string.IsNullOrWhiteSpace(wikiText)) return result;

        // Pega todos os blocos {{Dialogue Start}} ... {{Dialogue End}}
        foreach (Match blk in TextHelper.MatchDialogueBlocks(wikiText))
        {
            string body = blk.Groups["body"].Value;

            DialogueSection section = new DialogueSection(); // recomeça a cada contexto
            using StringReader reader = new StringReader(body);
            string? raw;
            while ((raw = reader.ReadLine()) != null)
            {
                string line = raw.TrimEnd();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("----")) continue;

                // Contexto ;( ... )
                if (TryExtractContext(line, out string context))
                {
                    // fecha seção anterior
                    if (!string.IsNullOrWhiteSpace(section.Context) || section.Lines.Count > 0)
                    {
                        result.Add(section);
                        section = new DialogueSection();
                    }
                    section.Context = TextHelper.CleanText(context);
                    continue;
                }

                // Linhas iniciadas por ":" (uma ou mais) = diálogo/choice
                if (!TryExtractDialogueRest(line, out string rest)) continue;

                // captura audios {{A|...}}
                TextHelper.ExtractAudioFiles(rest, out string? withoutAudio);
                string noAudio = withoutAudio?.Trim() ?? "";

                // é choice? começa com {{DIcon}}
                bool isChoice = TextHelper.StartsWithDialogueIcon(noAudio);
                if (isChoice)
                {
                    string txt = TextHelper.CleanText(noAudio);
                    if (!string.IsNullOrWhiteSpace(txt))
                        section.Lines.Add(new DialogueLine
                        {
                            Speaker = "[Choice]",
                            Text = txt.Replace("DIcon ","").Replace("\\\"", "")
                        });
                    continue;
                }

                // fala do tipo '''Speaker:''' Texto
                Match mTalk = DialogueSpeakerRegex().Match(noAudio);
                if (mTalk.Success)
                {
                    string sp = TextHelper.CleanText(mTalk.Groups["sp"].Value);
                    string tx = TextHelper.CleanText(mTalk.Groups["tx"].Value).Replace("\\\"", "");
                    section.Lines.Add(new DialogueLine
                    {
                        Speaker = string.IsNullOrWhiteSpace(sp) ? null : sp,
                        Text = string.IsNullOrWhiteSpace(tx) ? "" : tx
                    });
                }
                else
                {
                    // narrativa/linha solta sem speaker
                    string txt = TextHelper.CleanText(noAudio).Replace("\\\"", "");
                    if (!string.IsNullOrWhiteSpace(txt))
                        section.Lines.Add(new DialogueLine
                        {
                            Speaker = null,
                            Text = txt
                        });
                }
            }

            if (!string.IsNullOrWhiteSpace(section.Context) || section.Lines.Count > 0)
                result.Add(section);
        }

        return result;
    }

    private static bool TryExtractContext(string line, out string context)
    {
        context = string.Empty;
        string trimmed = line.Trim();
        if (!trimmed.StartsWith(";", StringComparison.Ordinal)) return false;

        string body = trimmed[1..].TrimStart();
        if (body.Length <= 2 || body[0] != '(' || body[^1] != ')') return false;

        context = body[1..^1];
        return context.Length > 0;
    }

    private static bool TryExtractDialogueRest(string line, out string rest)
    {
        rest = string.Empty;
        ReadOnlySpan<char> span = line.AsSpan().TrimStart();
        if (span.IsEmpty || span[0] != ':') return false;

        int i = 1;
        while (i < span.Length && span[i] == ':') i++;

        rest = span[i..].Trim().ToString();
        return true;
    }
}
