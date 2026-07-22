using AiVideoStudio.Application.Features.Exports.Models;
using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Application.Interfaces.Export;

public interface IExportGraphBuilder
{
    RenderGraph Build(
        Timeline timeline,
        IReadOnlyList<Track> tracks,
        IReadOnlyList<Clip> clips,
        IReadOnlyDictionary<string, MediaAsset> assets);
}
