using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Interfaces.Render;

public interface IProviderSelector
{
    IRenderProvider Select(RenderProvider preferredProvider);
}
