using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Background;

public interface IBackgroundJobService
{
    Task EnqueueAsync(string jobName, CancellationToken cancellationToken = default);
    Task EnqueueAsync<T>(T args, CancellationToken cancellationToken = default);
}
