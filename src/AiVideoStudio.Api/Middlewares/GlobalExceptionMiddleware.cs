using AiVideoStudio.Shared.Exceptions;
using AiVideoStudio.Shared.Responses;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AiVideoStudio.Shared.Logging;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using Asp.Versioning;

namespace AiVideoStudio.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAppLogger<GlobalExceptionMiddleware> logger)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(0, ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "An error occurred.",
            Errors = new[] { exception.Message },
            ErrorCode = "SERVER_ERROR",
            TraceId = Activity.Current?.Id,
            RequestId = context.TraceIdentifier,
            Version = context.GetRequestedApiVersion()?.ToString() ?? "1.0"
        };
        
        if (context.Items.TryGetValue("X-Correlation-Id", out var id))
        {
            response.CorrelationId = id?.ToString();
        }

        switch (exception)
        {
            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = Shared.Constants.ErrorMessages.ValidationFailed;
                response.ErrorCode = "VALIDATION_FAILED";
                response.Errors = validationEx.Errors.SelectMany(x => x.Value).ToList();
                break;
            case CustomException customEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = customEx.Message;
                response.ErrorCode = "CUSTOM_ERROR";
                break;
            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = Shared.Constants.ErrorMessages.InternalServerError;
                break;
        }

        var result = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return context.Response.WriteAsync(result);
    }
}
