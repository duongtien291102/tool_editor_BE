using AiVideoStudio.Domain.Entities;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Mongo;

public static class MongoIndexInitializer
{
    public static async Task InitializeAsync(MongoDbContext context, CancellationToken cancellationToken = default)
    {
        await CreateUserIndexesAsync(context.Users, cancellationToken);
        await CreateRefreshTokenIndexesAsync(context.RefreshTokens, cancellationToken);
        await CreateRoleIndexesAsync(context.Roles, cancellationToken);
        await CreatePermissionIndexesAsync(context.Permissions, cancellationToken);
        await CreateUserTokenIndexesAsync(context.Database.GetCollection<UserToken>("UserTokens"), cancellationToken);
        await CreatePasswordHistoryIndexesAsync(context.Database.GetCollection<PasswordHistory>("PasswordHistories"), cancellationToken);
        await CreateEmailOutboxIndexesAsync(context.Database.GetCollection<EmailOutbox>("EmailOutbox"), cancellationToken);
        await CreateAuditLogIndexesAsync(context.Database.GetCollection<AuditLog>("AuditLogs"), cancellationToken);

        // Sprint 4 — Timeline indexes
        await CreateTimelineIndexesAsync(context.Timelines, cancellationToken);
    }

    private static async Task CreateUserIndexesAsync(IMongoCollection<User> collection, CancellationToken cancellationToken)
    {
        var indexKeysDefinition = Builders<User>.IndexKeys;
        
        var partialFilter = Builders<User>.Filter.Eq(x => x.DeletedAt, null);

        var usernameIndexModel = new CreateIndexModel<User>(
            indexKeysDefinition.Ascending(x => x.Username),
            new CreateIndexOptions<User> { Unique = true, Sparse = true, PartialFilterExpression = partialFilter });

        var emailIndexModel = new CreateIndexModel<User>(
            indexKeysDefinition.Ascending(x => x.Email),
            new CreateIndexOptions<User> { Unique = true, Sparse = true, PartialFilterExpression = partialFilter });

        await collection.Indexes.CreateManyAsync(new[] { usernameIndexModel, emailIndexModel }, cancellationToken);
    }

    private static async Task CreateRefreshTokenIndexesAsync(IMongoCollection<RefreshToken> collection, CancellationToken cancellationToken)
    {
        var indexKeysDefinition = Builders<RefreshToken>.IndexKeys;

        var tokenHashIndexModel = new CreateIndexModel<RefreshToken>(
            indexKeysDefinition.Ascending(x => x.TokenHash),
            new CreateIndexOptions { Unique = true });

        var userIdIndexModel = new CreateIndexModel<RefreshToken>(
            indexKeysDefinition.Ascending(x => x.UserId));

        var familyIdIndexModel = new CreateIndexModel<RefreshToken>(
            indexKeysDefinition.Ascending(x => x.FamilyId));

        var ttlIndexModel = new CreateIndexModel<RefreshToken>(
            indexKeysDefinition.Ascending(x => x.ExpiresAt),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero });

        await collection.Indexes.CreateManyAsync(new[] 
        { 
            tokenHashIndexModel, 
            userIdIndexModel, 
            familyIdIndexModel, 
            ttlIndexModel 
        }, cancellationToken);
    }

    private static async Task CreateRoleIndexesAsync(IMongoCollection<Role> collection, CancellationToken cancellationToken)
    {
        var indexKeysDefinition = Builders<Role>.IndexKeys;

        var nameIndexModel = new CreateIndexModel<Role>(
            indexKeysDefinition.Ascending(x => x.Name),
            new CreateIndexOptions { Unique = true });

        await collection.Indexes.CreateOneAsync(nameIndexModel, cancellationToken: cancellationToken);
    }

    private static async Task CreatePermissionIndexesAsync(IMongoCollection<Permission> collection, CancellationToken cancellationToken)
    {
        var indexKeysDefinition = Builders<Permission>.IndexKeys;

        var codeIndexModel = new CreateIndexModel<Permission>(
            indexKeysDefinition.Ascending(x => x.Code),
            new CreateIndexOptions { Unique = true });

        await collection.Indexes.CreateOneAsync(codeIndexModel, cancellationToken: cancellationToken);
    }

    private static async Task CreateUserTokenIndexesAsync(IMongoCollection<UserToken> collection, CancellationToken cancellationToken)
    {
        var indexKeysDefinition = Builders<UserToken>.IndexKeys;

        var tokenHashIndexModel = new CreateIndexModel<UserToken>(
            indexKeysDefinition.Ascending(x => x.TokenHash));

        var ttlIndexModel = new CreateIndexModel<UserToken>(
            indexKeysDefinition.Ascending(x => x.ExpiresAt),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero });

        await collection.Indexes.CreateManyAsync(new[] { tokenHashIndexModel, ttlIndexModel }, cancellationToken);
    }

    private static async Task CreatePasswordHistoryIndexesAsync(IMongoCollection<PasswordHistory> collection, CancellationToken cancellationToken)
    {
        var indexKeysDefinition = Builders<PasswordHistory>.IndexKeys;

        var userIdIndexModel = new CreateIndexModel<PasswordHistory>(
            indexKeysDefinition.Ascending(x => x.UserId));

        await collection.Indexes.CreateOneAsync(userIdIndexModel, cancellationToken: cancellationToken);
    }

    private static async Task CreateEmailOutboxIndexesAsync(IMongoCollection<EmailOutbox> collection, CancellationToken cancellationToken)
    {
        var indexKeysDefinition = Builders<EmailOutbox>.IndexKeys;

        var statusRetryIndexModel = new CreateIndexModel<EmailOutbox>(
            indexKeysDefinition.Ascending(x => x.Status).Ascending(x => x.NextRetryAt));

        await collection.Indexes.CreateOneAsync(statusRetryIndexModel, cancellationToken: cancellationToken);
    }

    private static async Task CreateAuditLogIndexesAsync(IMongoCollection<AuditLog> collection, CancellationToken cancellationToken)
    {
        var indexKeysDefinition = Builders<AuditLog>.IndexKeys;

        var userIdTimestampIndexModel = new CreateIndexModel<AuditLog>(
            indexKeysDefinition.Ascending(x => x.UserId).Descending(x => x.Timestamp));

        await collection.Indexes.CreateOneAsync(userIdTimestampIndexModel, cancellationToken: cancellationToken);
    }

    // Checklist 7 — Timeline indexes for Search, Paging, Owner Query
    private static async Task CreateTimelineIndexesAsync(IMongoCollection<Timeline> collection, CancellationToken cancellationToken)
    {
        var keys = Builders<Timeline>.IndexKeys;
        var notDeleted = Builders<Timeline>.Filter.Eq(x => x.DeletedAt, null);

        // Unique: one active timeline per project (enforced only for non-deleted documents)
        var projectIdUniqueIndex = new CreateIndexModel<Timeline>(
            keys.Ascending(x => x.ProjectId),
            new CreateIndexOptions<Timeline>
            {
                Unique = true,
                Name = "idx_timeline_projectId_unique_active",
                PartialFilterExpression = notDeleted
            });

        // OwnerId — fast owner queries
        var ownerIdIndex = new CreateIndexModel<Timeline>(
            keys.Ascending(x => x.OwnerId),
            new CreateIndexOptions { Name = "idx_timeline_ownerId" });

        // DeletedAt — used in all queries to filter soft-deleted docs
        var deletedAtIndex = new CreateIndexModel<Timeline>(
            keys.Ascending(x => x.DeletedAt),
            new CreateIndexOptions { Name = "idx_timeline_deletedAt" });

        // CreatedAt — default sort for paging
        var createdAtIndex = new CreateIndexModel<Timeline>(
            keys.Descending(x => x.CreatedAt),
            new CreateIndexOptions { Name = "idx_timeline_createdAt_desc" });

        // UpdatedAt — sort support
        var updatedAtIndex = new CreateIndexModel<Timeline>(
            keys.Descending(x => x.UpdatedAt),
            new CreateIndexOptions { Name = "idx_timeline_updatedAt_desc" });

        // Compound: (OwnerId, DeletedAt) — owner query with soft-delete filter
        var ownerDeletedAtIndex = new CreateIndexModel<Timeline>(
            keys.Ascending(x => x.OwnerId).Ascending(x => x.DeletedAt),
            new CreateIndexOptions { Name = "idx_timeline_ownerId_deletedAt" });

        // Compound: (DeletedAt, CreatedAt) — paging with soft-delete filter
        var deletedAtCreatedAtIndex = new CreateIndexModel<Timeline>(
            keys.Ascending(x => x.DeletedAt).Descending(x => x.CreatedAt),
            new CreateIndexOptions { Name = "idx_timeline_deletedAt_createdAt" });

        await collection.Indexes.CreateManyAsync(new[]
        {
            projectIdUniqueIndex,
            ownerIdIndex,
            deletedAtIndex,
            createdAtIndex,
            updatedAtIndex,
            ownerDeletedAtIndex,
            deletedAtCreatedAtIndex
        }, cancellationToken);
    }
}

