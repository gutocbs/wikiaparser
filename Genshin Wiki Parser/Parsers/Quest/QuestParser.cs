using Genshin.Wiki.Parser.Helpers;
using Genshin.Wiki.Parser.Models.Quest;
using System.Text.RegularExpressions;

namespace Genshin.Wiki.Parser.Parsers.Quest;

public static partial class QuestParser
{
    [GeneratedRegex(@"'''\s*(?<sp>[^:'\n]+?)\s*:\s*'''\s*(?<tx>.*)$")]
    private static partial Regex DialogueSpeakerRegex();

    public static QuestDto? TryParse(string wikiText, string pageTitle)
    {
        if (string.IsNullOrWhiteSpace(wikiText)) return null;
        if (!wikiText.Contains("{{Quest Infobox", StringComparison.OrdinalIgnoreCase)) return null;

        var dto = new QuestDto { Title = pageTitle };

        // 1) Infobox
        var infobox = TextHelper.ExtractTemplate("Quest Infobox", wikiText);
        var map  = TextHelper.ParseTemplateParams(infobox);

        dto.Id           = TextHelper.TryInt(TextHelper.Get(map, "id"));
        dto.Type         = TextHelper.Get(map, "type");
        dto.Chapter      = TextHelper.Get(map, "chapter");
        dto.ActNum       = TextHelper.TryInt(TextHelper.Get(map, "actNum"));
        dto.Act          = TextHelper.Get(map, "act");
        dto.Part         = TextHelper.TryInt(TextHelper.Get(map, "part"));
        dto.Character    = TextHelper.CleanText(TextHelper.Get(map, "character"));

        dto.StartLocation= TextHelper.CleanText(TextHelper.Get(map, "startLocation"));
        dto.Region       = TextHelper.CleanText(TextHelper.Get(map, "region"));
        dto.Area         = TextHelper.CleanText(TextHelper.Get(map, "area"));
        dto.Subarea      = TextHelper.CleanText(TextHelper.Get(map, "subarea")).Replace(" (Subarea)", "");
        
        // elenco (separado por ';')
        var chars = TextHelper.Get(map, "characters");
        if (!string.IsNullOrWhiteSpace(chars))
        {
            foreach (var c in chars.Split(';'))
            {
                var v = TextHelper.CleanText(c);
                if (!string.IsNullOrWhiteSpace(v))
                    dto.Characters.Add(v.Trim());
            }
        }

        // 3) Descrição da Quest (template)
        dto.Description = ExtractQuestDescription(wikiText);

        // 4) Diálogos
        dto.Dialogues = ExtractDialogues(wikiText);

        return dto;
    }

    
    // ---------- Partes específicas de Quest ----------
    private static string? ExtractQuestDescription(string text)
    {
        foreach (var t in TextHelper.ExtractTemplates("Quest Description", text))
        {
            var inner = TextHelper.CleanText(t);
            if (!string.IsNullOrWhiteSpace(inner)) return inner.Replace("|","");
        }
        return null;
    }

    private static List<DialogueSection> ExtractDialogues(string text)
    {
        var result = new List<DialogueSection>();

        // pega blocos entre {{Dialogue Start}} ... {{Dialogue End}}
        foreach (Match blk in TextHelper.MatchDialogueBlocks(text))
        {
            var body = blk.Groups["body"].Value;
            var section = new DialogueSection();

            using var reader = new StringReader(body);
            string? raw;
            while ((raw = reader.ReadLine()) != null)
            {
                var line = raw.TrimEnd();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("----")) continue;

                // Contexto de cena: ;( ... )
                if (TryExtractContext(line, out var context))
                {
                    // inicia nova seção quando encontrar próximo contexto
                    if (!string.IsNullOrWhiteSpace(section.Context) || section.Lines.Count > 0)
                    {
                        result.Add(section);
                        section = new DialogueSection();
                    }
                    section.Context = TextHelper.CleanText(context);
                    continue;
                }

                // Linhas de diálogo/choices começam com ":"
                if (TryGetDialogueBody(line, out var dialogueBody))
                {
                    // Choice: :{{DIcon}} Text
                    if (TextHelper.StartsWithDialogueIcon(dialogueBody))
                    {
                        var textClean = TextHelper.CleanText(line);
                        if (!string.IsNullOrWhiteSpace(textClean))
                            section.Lines.Add(new DialogueLine { Speaker = "[Choice]", Text = textClean.Replace("DIcon","").Replace(":;","") });
                        continue;
                    }
                    
                    // Remove templates de áudio para facilitar parse do speaker/texto
                    var noAudio = TextHelper.RemoveAudioTemplates(line);

                    // Formato típico: : '''Speaker:''' Text
                    var mTalk = DialogueSpeakerRegex().Match(noAudio);
                    if (mTalk.Success)
                    {
                        var sp = TextHelper.CleanText(mTalk.Groups["sp"].Value);
                        var tx = TextHelper.CleanText(mTalk.Groups["tx"].Value);
                        if (!string.IsNullOrWhiteSpace(tx))
                            section.Lines.Add(new DialogueLine { Speaker = sp, Text = tx.Replace(":;","") });
                    }
                    else
                    {
                        // fallback: limpa wiki e joga como narrativa sem speaker
                        var txt = TextHelper.CleanText(noAudio);
                        if (!string.IsNullOrWhiteSpace(txt))
                            section.Lines.Add(new DialogueLine { Speaker = null, Text = txt.Replace(":;","") });
                    }
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
        var trimmed = line.Trim();
        if (!trimmed.StartsWith(";", StringComparison.Ordinal)) return false;

        var body = trimmed[1..].TrimStart();
        if (body.Length <= 2 || body[0] != '(' || body[^1] != ')') return false;

        context = body[1..^1];
        return context.Length > 0;
    }

    private static bool TryGetDialogueBody(string line, out string body)
    {
        body = string.Empty;
        var span = line.AsSpan().TrimStart();
        if (span.IsEmpty || span[0] != ':') return false;

        body = span[1..].TrimStart().ToString();
        return true;
    }
}
