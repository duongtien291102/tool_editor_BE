using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly IMongoCollection<Permission> _collection;

    public PermissionRepository(MongoDbContext context)
    {
        _collection = context.Permissions;
    }

    public Task<Permission?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken)!;
    }

    public async Task<IEnumerable<Permission>> GetPermissionsByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(Builders<Permission>.Filter.In(x => x.Id, ids)).ToListAsync(cancellationToken);
    }

    public Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        return _collection.InsertOneAsync(permission, new InsertOneOptions(), cancellationToken);
    }
}
