namespace AiVideoStudio.Shared.DomainErrors;

public static class AssetErrors
{
    public static readonly Error NotFound = new("ASSET.NOT_FOUND", "The asset was not found.");
}
