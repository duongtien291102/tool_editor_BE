using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IMongoCollection<RefreshToken> _collection;

    public RefreshTokenRepository(MongoDbContext context)
    {
        _collection = context.RefreshTokens;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _collection.Find(x => x.TokenHash == tokenHash).FirstOrDefaultAsync(cancellationToken)!;
    }

    public async Task<IEnumerable<RefreshToken>> GetFamilyAsync(string familyId, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.FamilyId == familyId).ToListAsync(cancellationToken);
    }

    public Task RevokeFamilyAsync(string familyId, string reason, string revokedByIp, CancellationToken cancellationToken = default)
    {
        var update = Builders<RefreshToken>.Update
            .Set(x => x.IsRevoked, true)
            .Set(x => x.ReasonRevoked, reason)
            .Set(x => x.RevokedByIp, revokedByIp)
            .Set(x => x.RevokedAt, System.DateTimeOffset.UtcNow);

        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.UpdateManyAsync(session, x => x.FamilyId == familyId && !x.IsRevoked, update, cancellationToken: cancellationToken);

        return _collection.UpdateManyAsync(x => x.FamilyId == familyId && !x.IsRevoked, update, cancellationToken: cancellationToken);
    }

    public Task RevokeAllForUserAsync(string userId, string reason, string revokedByIp, CancellationToken cancellationToken = default)
    {
        var update = Builders<RefreshToken>.Update
            .Set(x => x.IsRevoked, true)
            .Set(x => x.ReasonRevoked, reason)
            .Set(x => x.RevokedByIp, revokedByIp)
            .Set(x => x.RevokedAt, System.DateTimeOffset.UtcNow);

        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.UpdateManyAsync(session, x => x.UserId == userId && !x.IsRevoked, update, cancellationToken: cancellationToken);

        return _collection.UpdateManyAsync(x => x.UserId == userId && !x.IsRevoked, update, cancellationToken: cancellationToken);
    }

    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        return _collection.InsertOneAsync(token, new InsertOneOptions(), cancellationToken);
    }

    public Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        return _collection.ReplaceOneAsync(x => x.Id == token.Id, token, new ReplaceOptions(), cancellationToken);
    }
}
