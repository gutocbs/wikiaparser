using System.Reflection;
using System.Text;
using Genshin.Wiki.Parser.Enum;
using Genshin.Wiki.Parser.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Genshin.Wiki.Parser.Services;

// Sharda em JSON arrays. Cada shard vira um arquivo JSON válido.
public sealed class ShardedArraySink : IObjectSink
{
    private readonly string _outputDir;
    private readonly string _baseName;
    private readonly ShardMode _mode;
    private readonly Func<object, string> _keySelector;
    private readonly bool _pretty;
    private readonly int _maxPerFile; // usado só no modo Count

    // buffers em memória
    private readonly Dictionary<string, List<object>> _prefixBuckets; // Prefix
    private List<object> _countBuffer; // Count
    private int _countShardIndex = 0;

    public ShardedArraySink(
        string outputDir,
        string baseName,
        ShardMode shardMode,
        Func<object, string>? keySelector = null,
        bool pretty = true,
        int maxPerFile = 500 // só para Count
    )
    {
        Directory.CreateDirectory(outputDir);
        _outputDir = outputDir;
        _baseName = baseName;
        _mode = shardMode;
        _keySelector = keySelector ?? DefaultKeySelector;
        _pretty = pretty;
        _maxPerFile = Math.Max(1, maxPerFile);

        if (_mode == ShardMode.Prefix)
            _prefixBuckets = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);
        else
            _countBuffer = new List<object>(_maxPerFile);
    }

    public void Write(JsonSerializer serializer, object item)
    {
        if (item == null) return;

        switch (_mode)
        {
            case ShardMode.Prefix:
            {
                var key = SafeKey(_keySelector(item));
                var bucket = PrefixBucket(key);
                if (!_prefixBuckets.TryGetValue(bucket, out var list))
                    _prefixBuckets[bucket] = list = new List<object>();
                list.Add(item);
                break;
            }

            case ShardMode.Count:
            {
                _countBuffer.Add(item);
                if (_countBuffer.Count >= _maxPerFile)
                    FlushCountShard();
                break;
            }
        }
    }

    public void Dispose()
    {
        // grava o que restou
        if (_mode == ShardMode.Prefix)
        {
            foreach (var kv in _prefixBuckets.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var arr = new JArray(kv.Value.Select(JObject.FromObject));
                var path = Path.Combine(_outputDir, $"{_baseName}.{kv.Key}.json");
                File.WriteAllText(path, arr.ToString(_pretty ? Formatting.Indented : Formatting.None), new UTF8Encoding(false));
            }

            // manifest opcional
            var manifest = new JObject
            {
                ["type"] = _baseName,
                ["strategy"] = "array+prefix",
                ["createdAt"] = DateTime.UtcNow,
                ["totalShards"] = _prefixBuckets.Count,
                ["shards"] = new JObject(_prefixBuckets.Select(kv =>
                    new JProperty($"{_baseName}.{kv.Key}.json",
                        new JObject
                        {
                            ["count"] = kv.Value.Count,
                            ["prefixes"] = new JArray(kv.Key)
                        })))
            };
            File.WriteAllText(Path.Combine(_outputDir, $"{_baseName}.manifest.json"),
                manifest.ToString(Formatting.Indented), new UTF8Encoding(false));
        }
        else
        {
            if (_countBuffer.Count > 0) FlushCountShard();

            var files = Directory.EnumerateFiles(_outputDir, $"{_baseName}.*.json").ToList();
            var manifest = new JObject
            {
                ["type"] = _baseName,
                ["strategy"] = "array+count",
                ["createdAt"] = DateTime.UtcNow,
                ["totalShards"] = files.Count
            };
            File.WriteAllText(Path.Combine(_outputDir, $"{_baseName}.manifest.json"),
                manifest.ToString(Formatting.Indented), new UTF8Encoding(false));
        }
    }

    private void FlushCountShard()
    {
        if (_countBuffer.Count == 0) return;

        var arr = new JArray(_countBuffer.Select(JObject.FromObject));
        var path = Path.Combine(_outputDir, $"{_baseName}.{_countShardIndex:D4}.json");
        File.WriteAllText(path, arr.ToString(_pretty ? Formatting.Indented : Formatting.None), new UTF8Encoding(false));
        _countBuffer.Clear();
        _countShardIndex++;
    }

    private static string SafeKey(string? s) => string.IsNullOrWhiteSpace(s) ? "_" : s.Trim();

    private static string PrefixBucket(string key)
    {
        var c = key[0];
        if (char.IsLetter(c)) return char.ToUpperInvariant(c).ToString(); // A-Z
        if (char.IsDigit(c)) return "0-9";
        return "_"; // símbolos
    }

    // tenta achar Title/Name/Id por reflexão. Ajuste se quiser.
    private static string DefaultKeySelector(object o)
    {
        static string? TryProp(object x, string name)
        {
            var pi = x.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                      .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            return pi?.GetValue(x)?.ToString();
        }

        return TryProp(o, "Title")
            ?? TryProp(o, "Name")
            ?? TryProp(o, "Id")
            ?? Guid.NewGuid().ToString("N");
    }
}
