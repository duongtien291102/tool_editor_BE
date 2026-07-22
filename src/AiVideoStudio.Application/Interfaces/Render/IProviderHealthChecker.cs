using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Interfaces.Render;

public interface IProviderHealthChecker
{
    bool IsHealthy(RenderProvider provider);
}
