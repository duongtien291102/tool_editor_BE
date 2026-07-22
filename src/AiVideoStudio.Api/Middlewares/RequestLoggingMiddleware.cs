using AiVideoStudio.Shared.Logging;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiVideoStudio.Api.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAppLogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, IAppLogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            var request = context.Request;
            var response = context.Response;

            var correlationId = context.Items["X-Correlation-Id"]?.ToString();
            var userId = context.User?.Identity?.IsAuthenticated == true ? context.User.Identity.Name : "Anonymous";
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = request.Headers["User-Agent"].ToString();

            _logger.LogInformation(0, "HTTP Request Completed", new
            {
                Method = request.Method,
                Path = request.Path,
                StatusCode = response.StatusCode,
                DurationMs = sw.ElapsedMilliseconds,
                CorrelationId = correlationId,
                UserId = userId,
                IP = ipAddress,
                UserAgent = userAgent
            });
        }
    }
}
