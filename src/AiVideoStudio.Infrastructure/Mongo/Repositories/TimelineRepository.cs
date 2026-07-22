using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.Responses;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public class TimelineRepository : ITimelineRepository
{
    private const int MaxPageSize = 100;

    private readonly IMongoCollection<Timeline> _collection;

    // Checklist 4: Dictionary-based sort mapping — easy to extend without switch
    private static readonly Dictionary<string, Expression<Func<Timeline, object>>> SortFieldMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "name",      t => t.Name },
        { "updatedat", t => t.UpdatedAt! },
        { "createdat", t => t.CreatedAt },
    };

    public TimelineRepository(MongoDbContext context)
    {
        _collection = context.Timelines;
    }

    // Checklist 2: GetByIdAsync always excludes soft-deleted documents
    public Task<Timeline?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Timeline>.Filter.And(
            Builders<Timeline>.Filter.Eq(x => x.Id, id),
            Builders<Timeline>.Filter.Eq(x => x.DeletedAt, null)
        );

        return _collection.Find(filter).FirstOrDefaultAsync(cancellationToken)!;
    }

    // Checklist 2: GetByProjectIdAsync always excludes soft-deleted documents
    public Task<Timeline?> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Timeline>.Filter.And(
            Builders<Timeline>.Filter.Eq(x => x.ProjectId, projectId),
            Builders<Timeline>.Filter.Eq(x => x.DeletedAt, null)
        );

        return _collection.Find(filter).FirstOrDefaultAsync(cancellationToken)!;
    }

    // Checklist 3: ExistsByProjectIdAsync skips soft-deleted timelines
    public async Task<bool> ExistsByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Timeline>.Filter.And(
            Builders<Timeline>.Filter.Eq(x => x.ProjectId, projectId),
            Builders<Timeline>.Filter.Eq(x => x.DeletedAt, null)
        );

        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken) > 0;
    }

    public Task AddAsync(Timeline timeline, CancellationToken cancellationToken = default)
    {
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.InsertOneAsync(session, timeline, new InsertOneOptions(), cancellationToken);

        return _collection.InsertOneAsync(timeline, new InsertOneOptions(), cancellationToken);
    }

    // Checklist 1: UpdateAsync filter includes Id + Version + DeletedAt == null
    // This prevents updating a soft-deleted Timeline and implements Optimistic Concurrency.
    public async Task<bool> UpdateAsync(Timeline timeline, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Timeline>.Filter.And(
            Builders<Timeline>.Filter.Eq(x => x.Id, timeline.Id),
            Builders<Timeline>.Filter.Eq(x => x.Version, expectedVersion),
            Builders<Timeline>.Filter.Eq(x => x.DeletedAt, null)
        );

        ReplaceOneResult result;
        var session = MongoTransactionContext.CurrentSession;

        if (session != null)
        {
            result = await _collection.ReplaceOneAsync(session, filter, timeline, new ReplaceOptions(), cancellationToken);
        }
        else
        {
            result = await _collection.ReplaceOneAsync(filter, timeline, new ReplaceOptions(), cancellationToken);
        }

        // ModifiedCount == 0 means either Id not found, Version mismatch, or already soft-deleted → VersionConflict
        return result.ModifiedCount > 0;
    }

    // Soft Delete: the domain entity sets DeletedAt before calling this.
    // We use Id-only filter because the document is no longer "non-deleted" at this point.
    public Task DeleteAsync(Timeline timeline, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Timeline>.Filter.Eq(x => x.Id, timeline.Id);
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.ReplaceOneAsync(session, filter, timeline, new ReplaceOptions(), cancellationToken);

        return _collection.ReplaceOneAsync(filter, timeline, new ReplaceOptions(), cancellationToken);
    }

    // Checklist 2: GetPagedAsync always excludes soft-deleted documents
    // Checklist 4: Sort uses dictionary mapping instead of hard-coded switch
    // Checklist 5: Search input is Regex-escaped to prevent injection
    // Checklist 6: page and pageSize are normalized
    public async Task<PagedResult<Timeline>> GetPagedAsync(
        string? ownerId,
        bool isAdmin,
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        // Checklist 6 — normalize pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var filterBuilder = Builders<Timeline>.Filter;

        // Checklist 2 — always exclude soft-deleted at repository level
        var filter = filterBuilder.Eq(x => x.DeletedAt, null);

        // Owner / Admin filtering
        if (!string.IsNullOrEmpty(ownerId))
        {
            filter &= filterBuilder.Eq(x => x.OwnerId, ownerId);
        }

        // Checklist 5 — escape input before using in regex to prevent ReDoS/injection
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escapedSearch = Regex.Escape(search.Trim());
            var searchRegex = new BsonRegularExpression(escapedSearch, "i"); // "i" = case-insensitive
            filter &= filterBuilder.Regex(x => x.Name, searchRegex);
        }

        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        // Checklist 4 — dictionary-based sort, falls back to CreatedAt desc
        var sortDefinition = BuildSortDefinition(sortBy, sortDescending);

        var items = await _collection.Find(filter)
            .Sort(sortDefinition)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Timeline>(items, (int)totalCount, page, pageSize);
    }

    private static SortDefinition<Timeline> BuildSortDefinition(string? sortBy, bool sortDescending)
    {
        var builder = Builders<Timeline>.Sort;

        if (!string.IsNullOrWhiteSpace(sortBy) && SortFieldMap.TryGetValue(sortBy.Trim(), out var fieldExpr))
        {
            return sortDescending
                ? builder.Descending(fieldExpr)
                : builder.Ascending(fieldExpr);
        }

        // Default: CreatedAt descending (newest first)
        return sortDescending
            ? builder.Descending(t => t.CreatedAt)
            : builder.Ascending(t => t.CreatedAt);
    }
}
