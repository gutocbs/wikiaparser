using Genshin.Wiki.Parser.Helpers;

namespace Genshin.Wiki.Parser.Tests.Helpers;

[TestFixture]
public class ReadAheadPipelineTests
{
    [TestCase(1)]
    [TestCase(3)]
    [TestCase(1000)]
    public void Should_ConsumeAllItemsInSourceOrder_When_RunningPipeline(int capacity)
    {
        List<int> consumed = new();

        ReadAheadPipeline.Run(Enumerable.Range(0, 5000), consumed.Add, capacity);

        Assert.That(consumed, Is.EqualTo(Enumerable.Range(0, 5000)));
    }

    [Test]
    public void Should_ConsumeOnCallingThread_When_RunningPipeline()
    {
        int callingThread = Environment.CurrentManagedThreadId;
        HashSet<int> consumerThreads = new();
        int producerThread = -1;

        IEnumerable<int> Source()
        {
            producerThread = Environment.CurrentManagedThreadId;
            for (int i = 0; i < 100; i++) yield return i;
        }

        ReadAheadPipeline.Run(Source(), _ => consumerThreads.Add(Environment.CurrentManagedThreadId), 4);

        Assert.That(consumerThreads, Is.EqualTo(new[] { callingThread }));
        Assert.That(producerThread, Is.Not.EqualTo(callingThread), "a fonte deve ser lida em outra thread");
    }

    [Test]
    public void Should_DoNothing_When_SourceIsEmpty()
    {
        List<int> consumed = new();

        ReadAheadPipeline.Run(Array.Empty<int>(), consumed.Add, 2);

        Assert.That(consumed, Is.Empty);
    }

    [Test]
    public void Should_PropagateOriginalException_When_ProducerFails()
    {
        IEnumerable<int> Source()
        {
            yield return 1;
            throw new InvalidDataException("XML inválido");
        }

        InvalidDataException? ex = Assert.Throws<InvalidDataException>(() => ReadAheadPipeline.Run(Source(), _ => { }, 2));
        Assert.That(ex!.Message, Is.EqualTo("XML inválido"));
    }

    [Test]
    public void Should_StopProducerAndPropagate_When_ConsumerFails()
    {
        int produced = 0;
        bool sourceDisposed = false;

        IEnumerable<int> Source()
        {
            try
            {
                for (int i = 0; i < 1_000_000; i++)
                {
                    produced++;
                    yield return i;
                }
            }
            finally
            {
                sourceDisposed = true;
            }
        }

        Assert.Throws<InvalidOperationException>(() =>
            ReadAheadPipeline.Run(Source(), item =>
            {
                if (item == 10) throw new InvalidOperationException("falha no parse");
            }, 4));

        Assert.That(sourceDisposed, Is.True, "o leitor (fonte) precisa ser liberado");
        Assert.That(produced, Is.LessThan(1_000), "o produtor deve parar perto do ponto da falha (fila limitada)");
    }

    [Test]
    public void Should_Throw_When_CapacityIsNotPositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReadAheadPipeline.Run(new[] { 1 }, _ => { }, 0));
    }
}
