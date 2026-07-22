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

    public Task AddAsync(AuditLog entity, CancellationToken cancellationToken = default)
    {
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.InsertOneAsync(session, entity, new InsertOneOptions(), cancellationToken);
            
        return _collection.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);
    }
}
