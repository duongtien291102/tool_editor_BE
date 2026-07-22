using System.Threading.Tasks;

namespace AiVideoStudio.Application.Interfaces.Render;

/// <summary>
/// Abstraction for cancelling an active render job in the worker.
/// </summary>
public interface IRenderJobCanceller
{
    /// <summary>
    /// Attempts to cancel an actively processing job.
    /// </summary>
    void CancelActiveJob(string jobId);
}
