using System.Collections.Concurrent;
using System.Diagnostics;

namespace Genshin.Wiki.Parser.Helpers;

/// <summary>
/// Pipeline de 2 estágios: uma thread produz itens (ex.: lê páginas do XML) enquanto a thread chamadora os
/// consome na MESMA ordem. Há um único consumidor, então o consumo continua sequencial e sem concorrência.
/// A fila é limitada, o que limita a memória e faz o produtor esperar quando o consumidor está atrasado.
/// </summary>
public static class ReadAheadPipeline
{
    /// <summary>Executa o pipeline até o fim da fonte (ou até um erro de qualquer um dos lados).</summary>
    /// <param name="source">Fonte enumerada na thread produtora.</param>
    /// <param name="consume">Ação executada na thread chamadora, na ordem da fonte.</param>
    /// <param name="capacity">Itens máximos na fila entre os estágios (maior que zero).</param>
    /// <returns>Tempos de espera de cada lado (indicam qual estágio é o gargalo).</returns>
    public static PipelineStats Run<T>(IEnumerable<T> source, Action<T> consume, int capacity)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(consume);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        using BlockingCollection<T> queue = new(capacity);
        using CancellationTokenSource cancellation = new();
        long producerBlockedTicks = 0;

        Task producer = Task.Factory.StartNew(() =>
        {
            try
            {
                foreach (T item in source)
                {
                    long t0 = Stopwatch.GetTimestamp();
                    queue.Add(item, cancellation.Token); // bloqueia se a fila estiver cheia
                    producerBlockedTicks += Stopwatch.GetTimestamp() - t0;
                }
            }
            finally
            {
                queue.CompleteAdding();
            }
        }, cancellation.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        long consumerWaitTicks = 0;
        try
        {
            while (true)
            {
                long t0 = Stopwatch.GetTimestamp();
                bool hasItem = queue.TryTake(out T? item, Timeout.Infinite);
                consumerWaitTicks += Stopwatch.GetTimestamp() - t0;
                if (!hasItem) break; // fila concluída e vazia

                consume(item!);
            }
        }
        catch
        {
            // Erro no consumidor: libera o produtor (que pode estar bloqueado no Add) antes de propagar.
            cancellation.Cancel();
            try { producer.Wait(); } catch (AggregateException) { /* o erro relevante é o do consumidor */ }
            throw;
        }

        // Propaga o erro do produtor (ex.: XML inválido) com a exceção original.
        try
        {
            producer.Wait();
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
        }

        return new PipelineStats(producerBlockedTicks, consumerWaitTicks);
    }
}

/// <summary>Tempos de espera do pipeline, em ticks do Stopwatch.</summary>
/// <param name="ProducerBlockedTicks">Tempo do produtor esperando espaço na fila (consumidor é o gargalo).</param>
/// <param name="ConsumerWaitTicks">Tempo do consumidor esperando itens (produtor é o gargalo).</param>
public readonly record struct PipelineStats(long ProducerBlockedTicks, long ConsumerWaitTicks);
