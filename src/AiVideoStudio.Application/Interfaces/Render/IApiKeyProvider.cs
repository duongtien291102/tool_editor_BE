using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Interfaces.Render;

public interface IApiKeyProvider
{
    string? GetApiKey(RenderProvider provider);
    void SetApiKey(RenderProvider provider, string apiKey);
    bool RemoveApiKey(RenderProvider provider);
}
