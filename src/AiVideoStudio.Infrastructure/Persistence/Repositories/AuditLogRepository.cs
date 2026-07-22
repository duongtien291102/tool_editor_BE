using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Infrastructure.Mongo;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Persistence.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IMongoCollection<AuditLog> _collection;

    public AuditLogRepository(MongoDbContext context)
    {
        _collection = context.Database.GetCollection<AuditLog>("AuditLogs");
    }

    public AuditLogRepository(IMongoCollection<AuditLog> collection) => _collection = collection;

    public Task AddAsync(AuditLog entity, CancellationToken cancellationToken = default)
    {
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.InsertOneAsync(session, entity, new InsertOneOptions(), cancellationToken);
            
        return _collection.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);
    }

    public async Task<AiVideoStudio.Shared.Responses.PagedResult<AuditLog>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var total = await _collection.CountDocumentsAsync(FilterDefinition<AuditLog>.Empty, cancellationToken: cancellationToken);
        var items = await _collection.Find(FilterDefinition<AuditLog>.Empty).SortByDescending(x => x.Timestamp).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync(cancellationToken);
        return new(items, (int)total, page, pageSize);
    }
}
