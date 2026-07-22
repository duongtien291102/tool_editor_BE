using AiVideoStudio.Application.Features.Exports.Models;

namespace AiVideoStudio.Application.Interfaces.Export;

public interface IExportProvider
{
    Task<ExportProviderResult> ExportAsync(
        FFmpegCommandModel command,
        Func<ExportProgressUpdate, CancellationToken, Task> progress,
        CancellationToken cancellationToken = default);
}
