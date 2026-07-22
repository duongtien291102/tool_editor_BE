using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.Responses;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly IMongoCollection<Project> _collection;

    public ProjectRepository(MongoDbContext context)
    {
        _collection = context.Projects;
    }

    public Task<Project?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Project>.Filter.And(
            Builders<Project>.Filter.Eq(x => x.Id, id),
            Builders<Project>.Filter.Eq(x => x.DeletedAt, null)
        );

        return _collection.Find(filter).FirstOrDefaultAsync(cancellationToken)!;
    }

    public Task AddAsync(Project entity, CancellationToken cancellationToken = default)
    {
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.InsertOneAsync(session, entity, new InsertOneOptions(), cancellationToken);

        return _collection.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);
    }

    public Task UpdateAsync(Project entity, CancellationToken cancellationToken = default)
    {
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.ReplaceOneAsync(session, x => x.Id == entity.Id, entity, new ReplaceOptions(), cancellationToken);

        return _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, new ReplaceOptions(), cancellationToken);
    }

    public async Task<PagedResult<Project>> GetPagedAsync(
        string? ownerId,
        bool isAdmin,
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        bool sortDescending,
        ProjectStatus? status,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<Project>.Filter;
        var filter = filterBuilder.Eq(x => x.DeletedAt, null);

        if (!isAdmin && !string.IsNullOrEmpty(ownerId))
        {
            filter &= filterBuilder.Eq(x => x.OwnerId, ownerId);
        }
        else if (isAdmin && !string.IsNullOrEmpty(ownerId))
        {
            filter &= filterBuilder.Eq(x => x.OwnerId, ownerId);
        }

        if (status.HasValue)
        {
            filter &= filterBuilder.Eq(x => x.Status, status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchRegex = new BsonRegularExpression(search.Trim(), "i");
            var searchFilter = filterBuilder.Or(
                filterBuilder.Regex(x => x.Name, searchRegex),
                filterBuilder.Regex(x => x.Description, searchRegex)
            );
            filter &= searchFilter;
        }

        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var sortDefinition = GetSortDefinition(sortBy, sortDescending);

        var items = await _collection.Find(filter)
            .Sort(sortDefinition)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Project>(items, (int)totalCount, page, pageSize);
    }

    private static SortDefinition<Project> GetSortDefinition(string? sortBy, bool sortDescending)
    {
        var builder = Builders<Project>.Sort;
        var field = sortBy?.Trim().ToLowerInvariant();

        return field switch
        {
            "name" => sortDescending ? builder.Descending(x => x.Name) : builder.Ascending(x => x.Name),
            "updatedat" => sortDescending ? builder.Descending(x => x.UpdatedAt) : builder.Ascending(x => x.UpdatedAt),
            "status" => sortDescending ? builder.Descending(x => x.Status) : builder.Ascending(x => x.Status),
            _ => sortDescending ? builder.Descending(x => x.CreatedAt) : builder.Ascending(x => x.CreatedAt)
        };
    }
}
