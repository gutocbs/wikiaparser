using System.Text.RegularExpressions;
using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Character;
using Genshin.Wiki.Parser.Models.Quest;

namespace Genshin.Wiki.Parser.Parsers.Character;

public static class NpcParser
{
    private static readonly HashSet<string> FamilyKeys = new(StringComparer.OrdinalIgnoreCase)
        { "father", "sibling", "mother", "spouse", "child", "relative" };
    
    public static NpcDto? TryParse(string wikitext, string? title)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return null;
        if (!wikitext.Contains("{{Character Infobox", StringComparison.OrdinalIgnoreCase))
            return null;

        var box = TextHelper.ExtractTemplateBlock(wikitext, "Character Infobox");
        if (box is null) return null;

        var fields = TextHelper.ParseTemplateFields(box, "Character Infobox");

        string? type = TextHelper.CleanInline(TextHelper.Get(fields, "type"));
        // Se o tipo explicitamente diz NPC, seguimos; caso não tenha type, ainda dá pra aceitar (muitos NPCs têm).
        if (!string.IsNullOrWhiteSpace(type) && type.IndexOf("npc", StringComparison.OrdinalIgnoreCase) < 0)
        {
            // tem infobox de personagem, mas não é NPC → provavelmente Playable/Enemy/etc.
            return null;
        }

        var dto = new NpcDto
        {
            Name        = TextHelper.CleanInline(title),
            RealName    = TextHelper.CleanInline(TextHelper.Get(fields, "realname")),
            Type        = type,
            Element     = TextHelper.CleanInline(TextHelper.Get(fields, "element")),
            Region      = TextHelper.CleanInline(TextHelper.Get(fields, "region")),
            Locations   = TextHelper.ToList(TextHelper.Get(fields, "location")),
            Affiliations= TextHelper.ToList(TextHelper.Get(fields, "affiliation")),
            Title       = TextHelper.CleanInline(TextHelper.Get(fields, "title")),
            Deceased    = TextHelper.CleanInline(TextHelper.Get(fields, "deceased")),
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
            foreach (var family in familyFields)
            {
                var familyKey = family.Key;
                var familyValue = TextHelper.Get(fields, family.Key);
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
        var result = new List<DialogueSection>();
        if (string.IsNullOrWhiteSpace(wikiText)) return result;

        // Pega todos os blocos {{Dialogue Start}} ... {{Dialogue End}}
        var blockRx = new Regex(@"\{\{\s*Dialogue\s+Start\s*\}\}(?<body>[\s\S]*?)\{\{\s*Dialogue\s+End\s*\}\}",
                                RegexOptions.IgnoreCase);
        foreach (Match blk in blockRx.Matches(wikiText))
        {
            var body = blk.Groups["body"].Value;
            var lines = body.Split('\n');

            var section = new DialogueSection(); // recomeça a cada contexto
            foreach (var raw in lines)
            {
                var line = raw.TrimEnd();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("----")) continue;

                // Contexto ;( ... )
                var mCtx = Regex.Match(line, @"^\s*;\s*\((?<c>.+?)\)\s*$");
                if (mCtx.Success)
                {
                    // fecha seção anterior
                    if (!string.IsNullOrWhiteSpace(section.Context) || section.Lines.Count > 0)
                    {
                        result.Add(section);
                        section = new DialogueSection();
                    }
                    section.Context = TextHelper.CleanText(mCtx.Groups["c"].Value);
                    continue;
                }

                // Linhas iniciadas por ":" (uma ou mais) = diálogo/choice
                var mLead = Regex.Match(line, @"^\s*(?<colons>:+)\s*(?<rest>.*)$");
                if (!mLead.Success) continue;

                var rest  = mLead.Groups["rest"].Value.Trim();

                // captura audios {{A|...}}
                Regex.Matches(rest, @"\{\{\s*A\s*\|\s*([^}]+)\}\}", RegexOptions.IgnoreCase).Select(m => m.Groups[1].Value.Trim()).ToList();
                var noAudio = Regex.Replace(rest, @"\{\{\s*A\s*\|[^}]+\}\}", "", RegexOptions.IgnoreCase).Trim();

                // é choice? começa com {{DIcon}}
                var isChoice = Regex.IsMatch(noAudio, @"^\{\{\s*DIcon", RegexOptions.IgnoreCase);
                if (isChoice)
                {
                    var txt = TextHelper.CleanText(noAudio);
                    if (!string.IsNullOrWhiteSpace(txt))
                        section.Lines.Add(new DialogueLine
                        {
                            Speaker = "[Choice]",
                            Text = txt.Replace("DIcon ","").Replace("\\\"", "")
                        });
                    continue;
                }

                // fala do tipo '''Speaker:''' Texto
                var mTalk = Regex.Match(noAudio, @"^'''\s*(?<sp>[^:'\n]+?)\s*:\s*'''\s*(?<tx>.*)$");
                if (!mTalk.Success)
                    mTalk = Regex.Match(noAudio, @"^'''\s*(?<sp>[^:'\n]+?)\s*:\s*'''\s*$"); // casos com texto vazio

                if (mTalk.Success)
                {
                    var sp = TextHelper.CleanText(mTalk.Groups["sp"].Value);
                    var tx = TextHelper.CleanText(mTalk.Groups["tx"].Value).Replace("\\\"", "");
                    section.Lines.Add(new DialogueLine
                    {
                        Speaker = string.IsNullOrWhiteSpace(sp) ? null : sp,
                        Text = string.IsNullOrWhiteSpace(tx) ? "" : tx
                    });
                }
                else
                {
                    // narrativa/linha solta sem speaker
                    var txt = TextHelper.CleanText(noAudio).Replace("\\\"", "");
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
}
