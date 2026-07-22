using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Infrastructure.Render;

public sealed class RenderProviderFactory : IRenderProviderFactory
{
    private readonly IProviderSelector _selector;

    public RenderProviderFactory(IProviderSelector selector)
    {
        _selector = selector;
    }

    public IRenderProvider GetProvider(RenderProvider provider) => _selector.Select(provider);
}
