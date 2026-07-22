using AiVideoStudio.Shared.DomainErrors;
using System.Collections.Generic;
using System.Linq;

namespace AiVideoStudio.Shared.Responses;

public class Result
{
    public bool IsSuccess { get; }
    public Error Error { get; }
    public IEnumerable<string> ValidationErrors { get; }
    public IDictionary<string, object> Metadata { get; }

    protected Result(bool isSuccess, Error error, IEnumerable<string>? validationErrors, IDictionary<string, object>? metadata)
    {
        IsSuccess = isSuccess;
        Error = error ?? Error.None;
        ValidationErrors = validationErrors ?? Enumerable.Empty<string>();
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    public static Result Success() => new Result(true, Error.None, null, null);
    
    public static Result Failure(Error error, IEnumerable<string>? validationErrors = null, IDictionary<string, object>? metadata = null) 
        => new Result(false, error, validationErrors, metadata);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(T? value, bool isSuccess, Error error, IEnumerable<string>? validationErrors, IDictionary<string, object>? metadata) 
        : base(isSuccess, error, validationErrors, metadata)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new Result<T>(value, true, Error.None, null, null);
    
    public static new Result<T> Failure(Error error, IEnumerable<string>? validationErrors = null, IDictionary<string, object>? metadata = null) 
        => new Result<T>(default, false, error, validationErrors, metadata);
}
