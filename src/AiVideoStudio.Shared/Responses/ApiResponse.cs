using System;
using System.Collections.Generic;
using System.Linq;

namespace AiVideoStudio.Shared.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public IEnumerable<string> Errors { get; set; } = Enumerable.Empty<string>();
    public string? ErrorCode { get; set; }
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public string Timestamp { get; set; } = DateTimeOffset.UtcNow.ToString("O");
    public string? Version { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "")
    {
        return new ApiResponse<T> { Success = true, Data = data, Message = message };
    }

    public static ApiResponse<T> FailureResponse(string message, IEnumerable<string> errors, string? errorCode = null)
    {
        return new ApiResponse<T> { Success = false, Message = message, Errors = errors, ErrorCode = errorCode };
    }
}
