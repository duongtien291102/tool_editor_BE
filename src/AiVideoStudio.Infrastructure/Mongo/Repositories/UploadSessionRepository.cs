using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.Responses;
using MongoDB.Driver;
namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public sealed class UploadSessionRepository : IUploadSessionRepository
{
    private readonly IMongoCollection<UploadSession> _items; public UploadSessionRepository(MongoDbContext c) : this(c.UploadSessions) { }
    public UploadSessionRepository(IMongoCollection<UploadSession> i) => _items = i;
    public Task<UploadSession?> GetByIdAsync(string id, CancellationToken ct = default) => _items.Find(x => x.Id == id && x.DeletedAt == null).FirstOrDefaultAsync(ct)!;
    public Task AddAsync(UploadSession s, CancellationToken ct = default) => _items.InsertOneAsync(s, cancellationToken: ct);
    public async Task UpdateAsync(UploadSession s, CancellationToken ct = default) => await _items.ReplaceOneAsync(x => x.Id == s.Id, s, cancellationToken: ct);
    public async Task<PagedResult<UploadSession>> GetByProjectIdPagedAsync(string p, int page, int size, CancellationToken ct = default) { var f = Builders<UploadSession>.Filter.And(Builders<UploadSession>.Filter.Eq(x => x.ProjectId, p), Builders<UploadSession>.Filter.Eq(x => x.DeletedAt, null)); var count = await _items.CountDocumentsAsync(f, cancellationToken: ct); var data = await _items.Find(f).SortByDescending(x => x.CreatedAt).Skip((page - 1) * size).Limit(size).ToListAsync(ct); return new(data, (int)count, page, size); }
}
