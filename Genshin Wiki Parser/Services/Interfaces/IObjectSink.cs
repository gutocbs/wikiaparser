using Newtonsoft.Json;

namespace Genshin.Wiki.Parser.Services.Interfaces;

public interface IObjectSink : IDisposable
{
    void Write(JsonSerializer serializer, object item);
}