using AiVideoStudio.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Domain.Interfaces;

public interface IEmailOutboxRepository
{
    Task AddAsync(EmailOutbox entity, CancellationToken cancellationToken = default);
}
