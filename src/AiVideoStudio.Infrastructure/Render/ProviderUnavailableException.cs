using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Infrastructure.Render;

public sealed class ProviderUnavailableException : InvalidOperationException
{
    public ProviderUnavailableException(RenderProvider provider)
        : base($"No enabled and healthy render provider is available for '{provider}'.")
    {
        Provider = provider;
    }

    public RenderProvider Provider { get; }
}
