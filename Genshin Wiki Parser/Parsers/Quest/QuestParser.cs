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
        if (!wikiText.Contains($"{WikiSyntax.TemplateOpen}{WikiTemplates.QuestInfobox}", StringComparison.OrdinalIgnoreCase)) return null;

        QuestDto dto = new QuestDto { Title = pageTitle };

        // 1) Infobox
        string infobox = TextHelper.ExtractTemplate(WikiTemplates.QuestInfobox, wikiText);
        Dictionary<string, string> map  = TextHelper.ParseTemplateParams(infobox);

        dto.Id           = TextHelper.TryInt(TextHelper.Get(map, CommonFieldNames.Id));
        dto.Type         = TextHelper.Get(map, CommonFieldNames.Type);
        dto.Chapter      = TextHelper.Get(map, QuestFieldNames.Chapter);
        dto.ActNum       = TextHelper.TryInt(TextHelper.Get(map, QuestFieldNames.ActNum));
        dto.Act          = TextHelper.Get(map, QuestFieldNames.Act);
        dto.Part         = TextHelper.TryInt(TextHelper.Get(map, QuestFieldNames.Part));
        dto.Character    = TextHelper.CleanText(TextHelper.Get(map, QuestFieldNames.Character));

        dto.StartLocation= TextHelper.CleanText(TextHelper.Get(map, QuestFieldNames.StartLocation));
        dto.Region       = TextHelper.CleanText(TextHelper.Get(map, CommonFieldNames.Region));
        dto.Area         = TextHelper.CleanText(TextHelper.Get(map, CommonFieldNames.Area));
        dto.Subarea      = TextHelper.CleanText(TextHelper.Get(map, CommonFieldNames.Subarea)).Replace(" (Subarea)", "");
        
        // elenco (separado por ';')
        string? chars = TextHelper.Get(map, QuestFieldNames.Characters);
        if (!string.IsNullOrWhiteSpace(chars))
        {
            foreach (string c in chars.Split(';'))
            {
                string v = TextHelper.CleanText(c);
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
        foreach (string t in TextHelper.ExtractTemplates("Quest Description", text))
        {
            string inner = TextHelper.CleanText(t);
            if (!string.IsNullOrWhiteSpace(inner)) return inner.Replace("|","");
        }
        return null;
    }

    private static List<DialogueSection> ExtractDialogues(string text)
    {
        List<DialogueSection> result = new List<DialogueSection>();

        // pega blocos entre {{Dialogue Start}} ... {{Dialogue End}}
        foreach (Match blk in TextHelper.MatchDialogueBlocks(text))
        {
            string body = blk.Groups["body"].Value;
            DialogueSection section = new DialogueSection();

            using StringReader reader = new StringReader(body);
            string? raw;
            while ((raw = reader.ReadLine()) != null)
            {
                string line = raw.TrimEnd();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("----")) continue;

                // Contexto de cena: ;( ... )
                if (TryExtractContext(line, out string context))
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
                if (TryGetDialogueBody(line, out string dialogueBody))
                {
                    // Choice: :{{DIcon}} Text
                    if (TextHelper.StartsWithDialogueIcon(dialogueBody))
                    {
                        string textClean = TextHelper.CleanText(line);
                        if (!string.IsNullOrWhiteSpace(textClean))
                            section.Lines.Add(new DialogueLine { Speaker = "[Choice]", Text = textClean.Replace("DIcon","").Replace(":;","") });
                        continue;
                    }
                    
                    // Remove templates de áudio para facilitar parse do speaker/texto
                    string noAudio = TextHelper.RemoveAudioTemplates(line);

                    // Formato típico: : '''Speaker:''' Text
                    Match mTalk = DialogueSpeakerRegex().Match(noAudio);
                    if (mTalk.Success)
                    {
                        string sp = TextHelper.CleanText(mTalk.Groups["sp"].Value);
                        string tx = TextHelper.CleanText(mTalk.Groups["tx"].Value);
                        if (!string.IsNullOrWhiteSpace(tx))
                            section.Lines.Add(new DialogueLine { Speaker = sp, Text = tx.Replace(":;","") });
                    }
                    else
                    {
                        // fallback: limpa wiki e joga como narrativa sem speaker
                        string txt = TextHelper.CleanText(noAudio);
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
        string trimmed = line.Trim();
        if (!trimmed.StartsWith(";", StringComparison.Ordinal)) return false;

        string body = trimmed[1..].TrimStart();
        if (body.Length <= 2 || body[0] != '(' || body[^1] != ')') return false;

        context = body[1..^1];
        return context.Length > 0;
    }

    private static bool TryGetDialogueBody(string line, out string body)
    {
        body = string.Empty;
        ReadOnlySpan<char> span = line.AsSpan().TrimStart();
        if (span.IsEmpty || span[0] != ':') return false;

        body = span[1..].TrimStart().ToString();
        return true;
    }
}
