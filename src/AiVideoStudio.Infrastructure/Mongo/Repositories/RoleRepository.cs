using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IMongoCollection<Role> _collection;

    public RoleRepository(MongoDbContext context)
    {
        _collection = context.Roles;
    }

    public Task<Role?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken)!;
    }

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _collection.Find(x => x.Name == name).FirstOrDefaultAsync(cancellationToken)!;
    }

    public async Task<IEnumerable<Role>> GetRolesByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(Builders<Role>.Filter.In(x => x.Id, ids)).ToListAsync(cancellationToken);
    }

    public Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        return _collection.InsertOneAsync(role, new InsertOneOptions(), cancellationToken);
    }
}
