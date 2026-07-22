using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Interfaces;

public interface ITransactionManager
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task AbortTransactionAsync(CancellationToken cancellationToken = default);
}
