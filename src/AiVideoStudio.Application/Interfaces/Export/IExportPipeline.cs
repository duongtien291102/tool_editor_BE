using AiVideoStudio.Application.Features.Exports.Models;
using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Application.Interfaces.Export;

public interface IExportPipeline
{
    Task<ExportProviderResult> ExecuteAsync(
        ExportJob job,
        Func<ExportProgressUpdate, CancellationToken, Task> progress,
        CancellationToken cancellationToken = default);
}
