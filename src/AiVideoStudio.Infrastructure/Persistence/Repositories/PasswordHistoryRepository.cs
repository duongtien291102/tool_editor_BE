using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Infrastructure.Mongo;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Persistence.Repositories;

public class PasswordHistoryRepository : IPasswordHistoryRepository
{
    private readonly IMongoCollection<PasswordHistory> _collection;

    public PasswordHistoryRepository(MongoDbContext context)
    {
        _collection = context.Database.GetCollection<PasswordHistory>("PasswordHistories");
    }

    public async Task<System.Collections.Generic.IEnumerable<PasswordHistory>> GetRecentByUserIdAsync(string userId, int count = 5, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.UserId == userId)
            .SortByDescending(x => x.CreatedAt)
            .Limit(count)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(PasswordHistory entity, CancellationToken cancellationToken = default)
    {
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.InsertOneAsync(session, entity, new InsertOneOptions(), cancellationToken);
            
        return _collection.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);
    }
}
