using System.Threading;
using MongoDB.Driver;

namespace AiVideoStudio.Infrastructure.Mongo;

public static class MongoTransactionContext
{
    private static readonly AsyncLocal<IClientSessionHandle?> _currentSession = new();

    public static IClientSessionHandle? CurrentSession
    {
        get => _currentSession.Value;
        set => _currentSession.Value = value;
    }
}
