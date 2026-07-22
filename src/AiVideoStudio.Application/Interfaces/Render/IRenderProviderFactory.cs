using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Interfaces.Render;

public interface IRenderProviderFactory
{
    IRenderProvider GetProvider(RenderProvider provider);
}
