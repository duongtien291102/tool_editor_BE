using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Interfaces.Render;

public interface IRenderProviderRegistry
{
    void Register(IRenderProvider provider);
    bool TryGetProvider(RenderProvider provider, out IRenderProvider? implementation);
    IReadOnlyCollection<IRenderProvider> GetProviders();
}
