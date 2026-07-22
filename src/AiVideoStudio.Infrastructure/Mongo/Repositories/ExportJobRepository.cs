using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.Responses;
using MongoDB.Driver;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public sealed class ExportJobRepository : IExportJobRepository
{
    private readonly IMongoCollection<ExportJob> _collection;

    public ExportJobRepository(MongoDbContext context) : this(context.ExportJobs) { }
    public ExportJobRepository(IMongoCollection<ExportJob> collection) => _collection = collection;

    public Task<ExportJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        _collection.Find(job => job.Id == id && job.DeletedAt == null).FirstOrDefaultAsync(cancellationToken)!;

    public Task AddAsync(ExportJob job, CancellationToken cancellationToken = default) =>
        _collection.InsertOneAsync(job, cancellationToken: cancellationToken);

    public async Task UpdateAsync(ExportJob job, CancellationToken cancellationToken = default) =>
        await _collection.ReplaceOneAsync(item => item.Id == job.Id, job, cancellationToken: cancellationToken);

    public async Task<PagedResult<ExportJob>> GetByProjectIdPagedAsync(
        string projectId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<ExportJob>.Filter.And(
            Builders<ExportJob>.Filter.Eq(job => job.ProjectId, projectId),
            Builders<ExportJob>.Filter.Eq(job => job.DeletedAt, null));
        var total = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var items = await _collection.Find(filter)
            .SortByDescending(job => job.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<ExportJob>(items, (int)total, page, pageSize);
    }
}
