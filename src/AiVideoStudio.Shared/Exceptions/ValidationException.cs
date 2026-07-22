namespace AiVideoStudio.Shared.Exceptions;

public class ValidationException : CustomException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IDictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }
}
