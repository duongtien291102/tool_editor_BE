using AiVideoStudio.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace AiVideoStudio.Api.Services;

public class CurrentRequest : ICurrentRequest
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentRequest(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? CorrelationId 
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            if (context.Items.TryGetValue("X-Correlation-Id", out var id))
            {
                return id?.ToString();
            }

            if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var headerId))
            {
                return headerId.FirstOrDefault();
            }

            return null;
        }
    }
}
