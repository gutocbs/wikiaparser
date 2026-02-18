using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Genshin.Wiki.Parser.Services.Sink;

// Sharda em JSON arrays. Cada shard vira um arquivo JSON válido.

public sealed class SharedArraySink<T> : IDisposable
{
    public enum OversizeItemBehavior
    {
        Throw,
        AllowSingleFile // se um item sozinho exceder o limite, grava mesmo assim (não tem milagre)
    }

    private readonly string _outRoot;
    private readonly string _filePrefix;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Encoding _encoding;
    private readonly long _maxBytesPerFile;
    private readonly int _maxWordsPerFile;
    private readonly int _maxFiles;
    private readonly OversizeItemBehavior _oversizeBehavior;
    private readonly string _fileExtension;

    private FileStream? _fs;
    private int _fileIndex = 0;
    private bool _firstItemInFile = true;

    private long _currentBytes; // bytes efetivamente escritos no arquivo atual
    private int _currentWords;  // palavras acumuladas no arquivo atual

    public SharedArraySink(
        string outRoot,
        string filePrefix,
        long maxBytesPerFile,
        int maxWordsPerFile, string fileExtension, int maxFiles = 50,
        OversizeItemBehavior oversizeBehavior = OversizeItemBehavior.AllowSingleFile,
        JsonSerializerOptions? jsonOptions = null,
        Encoding? encoding = null)
    {
        if (string.IsNullOrWhiteSpace(outRoot)) throw new ArgumentException("outRoot is required.", nameof(outRoot));
        if (string.IsNullOrWhiteSpace(filePrefix)) throw new ArgumentException("filePrefix is required.", nameof(filePrefix));
        if (maxBytesPerFile <= 16) throw new ArgumentOutOfRangeException(nameof(maxBytesPerFile));
        if (maxWordsPerFile <= 0) throw new ArgumentOutOfRangeException(nameof(maxWordsPerFile));
        if (maxFiles <= 0) throw new ArgumentOutOfRangeException(nameof(maxFiles));

        _outRoot = outRoot;
        _filePrefix = filePrefix;
        _maxBytesPerFile = maxBytesPerFile;
        _maxWordsPerFile = maxWordsPerFile;
        _fileExtension = fileExtension;
        _maxFiles = maxFiles;
        _oversizeBehavior = oversizeBehavior;

        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            WriteIndented = false
        };

        // Vamos gravar em UTF-8 (padrão e o melhor pro tamanho)
        _encoding = encoding ?? Encoding.UTF8;

        Directory.CreateDirectory(_outRoot);
    }

    public void Write(T item)
    {
        EnsureFileOpen();

        // Serializa o item uma única vez (pra medir bytes e gravar sem re-serializar)
        byte[] itemUtf8 = JsonSerializer.SerializeToUtf8Bytes(item, _jsonOptions);

        // Contagem aproximada de “palavras” com base no JSON serializado
        // (NotebookLM não documenta exatamente, mas isso segura bem o limite)
        string itemText = _encoding.GetString(itemUtf8);
        int itemWords = CountWords(itemText);

        // Bytes extras por item (separador + item)
        int separatorBytes = _firstItemInFile ? 0 : 1; // ","
        int separatorWords = 0;

        // Pra garantir que o arquivo SEMPRE termina válido, reservamos 1 byte pro ']'
        long reservedCloseBracketBytes = 1;

        long projectedBytes = _currentBytes + separatorBytes + itemUtf8.Length + reservedCloseBracketBytes;
        int projectedWords = _currentWords + separatorWords + itemWords;

        bool exceedsBytes = projectedBytes > _maxBytesPerFile;
        bool exceedsWords = projectedWords > _maxWordsPerFile;

        if ((exceedsBytes || exceedsWords) && !_firstItemInFile)
        {
            // Fecha o atual e abre um novo
            CloseCurrentFile();
            EnsureFileOpen();

            // Recalcula no arquivo novo
            separatorBytes = 0;
            projectedBytes = _currentBytes + separatorBytes + itemUtf8.Length + reservedCloseBracketBytes;
            projectedWords = _currentWords + itemWords;

            exceedsBytes = projectedBytes > _maxBytesPerFile;
            exceedsWords = projectedWords > _maxWordsPerFile;
        }

        // Se mesmo num arquivo vazio excede, só tem duas opções: permitir ou lançar erro
        if ((exceedsBytes || exceedsWords) && _firstItemInFile)
        {
            if (_oversizeBehavior == OversizeItemBehavior.Throw)
            {
                throw new InvalidOperationException(
                    $"Single item exceeds limits. ItemBytes={itemUtf8.Length}, ItemWords={itemWords}, " +
                    $"MaxBytes={_maxBytesPerFile}, MaxWords={_maxWordsPerFile}. " +
                    $"Consider OversizeItemBehavior.AllowSingleFile.");
            }

            // AllowSingleFile: segue, vai estourar, mas é impossível não estourar.
        }

        // Escreve separador
        if (!_firstItemInFile)
        {
            _fs!.WriteByte((byte)',');
            _currentBytes += 1;
        }

        // Escreve item cru
        _fs!.Write(itemUtf8, 0, itemUtf8.Length);
        _currentBytes += itemUtf8.Length;

        _currentWords += itemWords;
        _firstItemInFile = false;
    }

    public void Flush()
    {
        if (_fs is null) return;
        _fs.Flush(true);
    }

    public void Dispose()
    {
        CloseCurrentFile();
        GC.SuppressFinalize(this);
    }

    private void EnsureFileOpen()
    {
        if (_fs != null) return;

        if (_fileIndex >= _maxFiles)
            throw new InvalidOperationException($"Max file count reached ({_maxFiles}). Increase maxFiles or increase per-file limits.");

        _fileIndex++;

        string path = Path.Combine(_outRoot, $"{_filePrefix}_{_fileIndex:D2}.{_fileExtension}");
        _fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);

        // Abre array
        _fs.WriteByte((byte)'[');
        _currentBytes = 1;
        _currentWords = 0;
        _firstItemInFile = true;
    }

    private void CloseCurrentFile()
    {
        if (_fs is null) return;

        // Fecha array (mesmo vazio)
        _fs.WriteByte((byte)']');
        _currentBytes += 1;

        _fs.Flush(true);
        _fs.Dispose();
        _fs = null;
    }

    private static int CountWords(string text)
    {
        // Contador simples e rápido:
        // conta “palavra” como sequência de letras/dígitos (Unicode).
        int count = 0;
        bool inWord = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            bool isWordChar = char.IsLetterOrDigit(c);

            if (isWordChar)
            {
                if (!inWord)
                {
                    inWord = true;
                    count++;
                }
            }
            else
            {
                inWord = false;
            }
        }

        return count;
    }
}

