using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Genshin.Wiki.Parser.Services.Serialization;

public sealed class SingleLineStringJsonConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(RemoveLineBreaks(value));
    }

    private static string RemoveLineBreaks(string value)
    {
        int firstLineBreak = value.AsSpan().IndexOfAny('\r', '\n');
        if (firstLineBreak < 0) return value;

        StringBuilder sb = new StringBuilder(value.Length);
        sb.Append(value.AsSpan(0, firstLineBreak));

        bool lastWasSpace = firstLineBreak > 0 && char.IsWhiteSpace(value[firstLineBreak - 1]);

        for (int i = firstLineBreak; i < value.Length; i++)
        {
            char c = value[i];
            if (c is '\r' or '\n')
            {
                if (!lastWasSpace)
                {
                    sb.Append(' ');
                    lastWasSpace = true;
                }

                continue;
            }

            sb.Append(c);
            lastWasSpace = char.IsWhiteSpace(c);
        }

        return sb.ToString().Trim();
    }
}
