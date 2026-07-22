using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Infrastructure.Mongo;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Persistence.Repositories;

public class UserTokenRepository : IUserTokenRepository
{
    private readonly IMongoCollection<UserToken> _collection;

    public UserTokenRepository(MongoDbContext context)
    {
        _collection = context.Database.GetCollection<UserToken>("UserTokens");
    }

    public Task<UserToken?> GetByHashAsync(string tokenHash, string purpose, CancellationToken cancellationToken = default)
    {
        return _collection.Find(x => x.TokenHash == tokenHash && x.Purpose == purpose).FirstOrDefaultAsync(cancellationToken)!;
    }

    public Task UpdateAsync(UserToken entity, CancellationToken cancellationToken = default)
    {
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.ReplaceOneAsync(session, x => x.Id == entity.Id, entity, new ReplaceOptions(), cancellationToken);
            
        return _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, new ReplaceOptions(), cancellationToken);
    }

    public Task AddAsync(UserToken entity, CancellationToken cancellationToken = default)
    {
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.InsertOneAsync(session, entity, new InsertOneOptions(), cancellationToken);
            
        return _collection.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);
    }
}
