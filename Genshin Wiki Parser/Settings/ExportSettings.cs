using System.Text.Json;
using System.Text.Json.Serialization;
using Genshin.Wiki.Parser.Services.Serialization;
using Genshin.Wiki.Parser.Services.Sink;

namespace Genshin.Wiki.Parser.Settings;

public sealed class ExportSettings
{
    public string DefaultFileExtension { get; set; } = "txt";
    public ShardedArraySinkSettings ShardedArraySink { get; set; } = new();
}

public sealed class ShardedArraySinkSettings
{
    public long MaxBytesPerFile { get; set; } = 200L * 1024 * 1024;
    public int MaxWordsPerFile { get; set; } = 500_000;
    public int MaxFiles { get; set; } = 50;
    public SharedArraySink<object>.OversizeItemBehavior OversizeItemBehavior { get; set; } = SharedArraySink<object>.OversizeItemBehavior.AllowSingleFile;
    public bool WriteIndented { get; set; }
    public bool IgnoreNullValues { get; set; } = true;
    public bool RemoveStringLineBreaks { get; set; } = true;

    public JsonSerializerOptions CreateJsonSerializerOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = WriteIndented,
            DefaultIgnoreCondition = IgnoreNullValues
                ? JsonIgnoreCondition.WhenWritingNull
                : JsonIgnoreCondition.Never
        };

        if (RemoveStringLineBreaks) 
            options.Converters.Add(new SingleLineStringJsonConverter());

        return options;
    }
}
