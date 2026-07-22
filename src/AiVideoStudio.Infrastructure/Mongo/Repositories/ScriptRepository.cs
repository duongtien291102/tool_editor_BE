using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.Responses;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public class ScriptRepository : IScriptRepository
{
    private readonly IMongoCollection<Script> _collection;

    public ScriptRepository(MongoDbContext context)
    {
        _collection = context.Scripts;
    }

    public Task<Script?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Script>.Filter.And(
            Builders<Script>.Filter.Eq(x => x.Id, id),
            Builders<Script>.Filter.Eq(x => x.DeletedAt, null)
        );

        return _collection.Find(filter).FirstOrDefaultAsync(cancellationToken)!;
    }

    public Task<Script?> GetForUpdateAsync(string id, CancellationToken cancellationToken = default)
    {
        // MongoDB doesn't have a traditional pessimistic row lock like SQL Server (SELECT FOR UPDATE).
        // Optimistic concurrency is handled during UpdateAsync using the expectedVersion.
        // Therefore, this method just fetches the document.
        return GetByIdAsync(id, cancellationToken);
    }

    public async Task<PagedResult<Script>> GetScriptsByProjectAsync(
        string projectId,
        string? searchTerm = null,
        bool includeDeleted = false,
        string? sortBy = null,
        bool descending = true,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<Script>.Filter;
        var filter = filterBuilder.Eq(x => x.ProjectId, projectId);

        if (!includeDeleted)
        {
            filter &= filterBuilder.Eq(x => x.DeletedAt, null);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchRegex = new BsonRegularExpression(searchTerm.Trim(), "i");
            var searchFilter = filterBuilder.Or(
                filterBuilder.Regex(x => x.Name, searchRegex),
                filterBuilder.Regex(x => x.Description, searchRegex)
            );
            filter &= searchFilter;
        }

        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var builder = Builders<Script>.Sort;
        var field = sortBy?.Trim().ToLowerInvariant();

        var sortDefinition = field switch
        {
            "name" => descending ? builder.Descending(x => x.Name) : builder.Ascending(x => x.Name),
            "updatedat" => descending ? builder.Descending(x => x.UpdatedAt) : builder.Ascending(x => x.UpdatedAt),
            _ => descending ? builder.Descending(x => x.CreatedAt) : builder.Ascending(x => x.CreatedAt)
        };

        var items = await _collection.Find(filter)
            .Sort(sortDefinition)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Script>(items, (int)totalCount, pageNumber, pageSize);
    }

    public Task AddAsync(Script script, CancellationToken cancellationToken = default)
    {
        return _collection.InsertOneAsync(script, new InsertOneOptions(), cancellationToken);
    }

    public async Task UpdateAsync(Script script, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Script>.Filter.And(
            Builders<Script>.Filter.Eq(x => x.Id, script.Id),
            Builders<Script>.Filter.Eq(x => x.Version, expectedVersion)
        );

        var result = await _collection.ReplaceOneAsync(filter, script, new ReplaceOptions(), cancellationToken);

        if (result.MatchedCount == 0)
        {
            // The document was not found with the expected version. It was either deleted or modified concurrently.
            throw new MongoException("Concurrency violation: The script has been modified by another user since it was loaded.");
        }
    }

    public async Task SoftDeleteAsync(Script script, CancellationToken cancellationToken = default)
    {
        // For soft delete, we still need to enforce concurrency (use script.Version - 1 because SoftDelete bumps the version).
        var expectedVersion = script.Version - 1;
        
        var filter = Builders<Script>.Filter.And(
            Builders<Script>.Filter.Eq(x => x.Id, script.Id),
            Builders<Script>.Filter.Eq(x => x.Version, expectedVersion)
        );

        var result = await _collection.ReplaceOneAsync(filter, script, new ReplaceOptions(), cancellationToken);

        if (result.MatchedCount == 0)
        {
            throw new MongoException("Concurrency violation: The script has been modified by another user since it was loaded.");
        }
    }
}
