using System.Text.Json;
using System.Text.Json.Serialization;
using Genshin.Wiki.Parser.Services.Sink;

namespace Genshin.Wiki.Parser.Configuration;

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

    public JsonSerializerOptions CreateJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = WriteIndented,
            DefaultIgnoreCondition = IgnoreNullValues
                ? JsonIgnoreCondition.WhenWritingNull
                : JsonIgnoreCondition.Never
        };
    }
}
