namespace AiVideoStudio.Shared.DomainErrors;

public static class GeneralErrors
{
    public static readonly Error ConcurrencyException = new("GENERAL.CONCURRENCY_EXCEPTION", "A concurrency conflict occurred. Please retry.");
}
