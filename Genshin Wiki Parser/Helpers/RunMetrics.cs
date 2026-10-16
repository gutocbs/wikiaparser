using System.Diagnostics;

namespace Genshin.Wiki.Parser.Helpers;

/// <summary>
/// Métricas de execução impressas no console ao fim do processamento (não afetam a saída).
/// Para cada fase registra tempo de relógio e tempo de CPU; "cores" = CPU / relógio (média de núcleos em uso).
/// </summary>
public static class RunMetrics
{
    private static readonly Stopwatch Total = Stopwatch.StartNew();
    private static readonly Stopwatch Phase = Stopwatch.StartNew();
    private static readonly List<(string Name, long WallMs, long CpuMs)> Phases = new();
    private static TimeSpan _phaseCpuStart = CurrentCpu();
    private static int _peakThreads;

    /// <summary>Reinicia os cronômetros no começo da execução.</summary>
    public static void Start()
    {
        Total.Restart();
        Phase.Restart();
        _phaseCpuStart = CurrentCpu();
    }

    /// <summary>Fecha a fase atual, registrando tempo de relógio e de CPU desde a fase anterior.</summary>
    /// <param name="name">Nome da fase (ex.: "readAndParse").</param>
    public static void EndPhase(string name)
    {
        TimeSpan cpu = CurrentCpu();
        Phases.Add((name, Phase.ElapsedMilliseconds, (long)(cpu - _phaseCpuStart).TotalMilliseconds));
        SampleThreads();
        _phaseCpuStart = cpu;
        Phase.Restart();
    }

    private static readonly Dictionary<string, long> Timers = new();

    /// <summary>Acumula um intervalo medido com Stopwatch.GetTimestamp() num contador nomeado.</summary>
    /// <param name="name">Nome do contador (ex.: "read", "parse").</param>
    /// <param name="elapsedTicks">Intervalo em ticks do Stopwatch.</param>
    public static void AddTime(string name, long elapsedTicks)
    {
        lock (Timers)
            Timers[name] = Timers.GetValueOrDefault(name) + elapsedTicks;
    }

    /// <summary>Registra o número atual de threads do processo (guarda o pico).</summary>
    public static void SampleThreads()
    {
        using Process process = Process.GetCurrentProcess();
        _peakThreads = Math.Max(_peakThreads, process.Threads.Count);
    }

    /// <summary>Imprime o resumo da execução.</summary>
    public static void Print()
    {
        using Process process = Process.GetCurrentProcess();
        long totalMs = Total.ElapsedMilliseconds;
        long cpuMs = (long)process.TotalProcessorTime.TotalMilliseconds;

        Console.WriteLine("---- Run metrics ----");
        foreach ((string name, long wallMs, long phaseCpuMs) in Phases)
            Console.WriteLine($"phase.{name}: wall.ms = {wallMs}, cpu.ms = {phaseCpuMs}, cores = {Cores(phaseCpuMs, wallMs)}");
        lock (Timers)
            foreach ((string name, long ticks) in Timers)
                Console.WriteLine($"time.{name}.ms = {ticks * 1000 / Stopwatch.Frequency}");
        Console.WriteLine($"total: wall.ms = {totalMs}, cpu.ms = {cpuMs}, cores = {Cores(cpuMs, totalMs)}");
        Console.WriteLine($"peakWorkingSet.mb = {process.PeakWorkingSet64 / (1024 * 1024)}");
        Console.WriteLine($"peakThreads = {Math.Max(_peakThreads, process.Threads.Count)}, logicalProcessors = {Environment.ProcessorCount}");
        Console.WriteLine($"gc.gen0 = {GC.CollectionCount(0)}, gc.gen1 = {GC.CollectionCount(1)}, gc.gen2 = {GC.CollectionCount(2)}");
        Console.WriteLine($"allocated.mb = {GC.GetTotalAllocatedBytes() / (1024 * 1024)}");
    }

    private static string Cores(long cpuMs, long wallMs)
        => wallMs <= 0 ? "0" : (cpuMs / (double)wallMs).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

    private static TimeSpan CurrentCpu()
    {
        using Process process = Process.GetCurrentProcess();
        return process.TotalProcessorTime;
    }
}
