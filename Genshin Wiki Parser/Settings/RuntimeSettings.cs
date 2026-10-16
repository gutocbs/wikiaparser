namespace Genshin.Wiki.Parser.Settings;

public sealed class RuntimeSettings
{
    public RuntimeDefaults Defaults { get; set; } = new();
    public ProcessingSettings Processing { get; set; } = new();
}

public sealed class ProcessingSettings
{
    // Lotes de páginas que a thread de leitura pode adiantar em relação ao parse.
    // 0 = sem thread de leitura (leitura e parse em sequência, numa thread só).
    public int ReaderQueueBatches { get; set; } = 8;

    // Páginas por lote entregue da thread de leitura para o parse.
    public int ReaderBatchSize { get; set; } = 64;
}

public sealed class RuntimeDefaults
{
    public string InputFile { get; set; } = "gensinimpact_pages_current.xml";
    public string OutputPath { get; set; } = "files";
    public string IgnoreListPath { get; set; } = "IgnoreList.json";
}
