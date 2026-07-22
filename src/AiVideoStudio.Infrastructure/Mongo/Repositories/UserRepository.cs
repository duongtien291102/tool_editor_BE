using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _collection;

    public UserRepository(MongoDbContext context)
    {
        _collection = context.Users;
    }

    public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken)!;
    }

    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return _collection.Find(x => x.Username == username).FirstOrDefaultAsync(cancellationToken)!;
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _collection.Find(x => x.Email == email).FirstOrDefaultAsync(cancellationToken)!;
    }

    public async Task<bool> ExistsUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.Username == username).AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.Email == email).AnyAsync(cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.InsertOneAsync(session, user, new InsertOneOptions(), cancellationToken);
            
        return _collection.InsertOneAsync(user, new InsertOneOptions(), cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.ReplaceOneAsync(session, x => x.Id == user.Id, user, new ReplaceOptions(), cancellationToken);
            
        return _collection.ReplaceOneAsync(x => x.Id == user.Id, user, new ReplaceOptions(), cancellationToken);
    }
}
