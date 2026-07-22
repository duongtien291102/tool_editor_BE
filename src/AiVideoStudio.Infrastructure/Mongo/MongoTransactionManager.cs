using AiVideoStudio.Application.Interfaces;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Mongo;

public class MongoTransactionManager : ITransactionManager, IDisposable
{
    private readonly MongoDbContext _context;
    private IClientSessionHandle? _session;

    public MongoTransactionManager(MongoDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_session != null) return; // Already started

        _session = await _context.Client.StartSessionAsync(cancellationToken: cancellationToken);
        _session.StartTransaction();
        
        MongoTransactionContext.CurrentSession = _session;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_session == null) return;

        await _session.CommitTransactionAsync(cancellationToken);
        
        MongoTransactionContext.CurrentSession = null;
        _session.Dispose();
        _session = null;
    }

    public async Task AbortTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_session == null) return;

        if (_session.IsInTransaction)
        {
            await _session.AbortTransactionAsync(cancellationToken);
        }
        
        MongoTransactionContext.CurrentSession = null;
        _session.Dispose();
        _session = null;
    }

    public void Dispose()
    {
        if (_session != null)
        {
            if (_session.IsInTransaction)
            {
                _session.AbortTransaction();
            }
            _session.Dispose();
            _session = null;
            MongoTransactionContext.CurrentSession = null;
        }
    }
}
