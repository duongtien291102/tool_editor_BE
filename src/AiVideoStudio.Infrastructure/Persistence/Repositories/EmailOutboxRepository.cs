using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Infrastructure.Mongo;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Persistence.Repositories;

public class EmailOutboxRepository : IEmailOutboxRepository
{
    private readonly IMongoCollection<EmailOutbox> _collection;

    public EmailOutboxRepository(MongoDbContext context)
    {
        _collection = context.Database.GetCollection<EmailOutbox>("EmailOutbox");
    }

    public Task AddAsync(EmailOutbox entity, CancellationToken cancellationToken = default)
    {
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.InsertOneAsync(session, entity, new InsertOneOptions(), cancellationToken);
            
        return _collection.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);
    }
}
