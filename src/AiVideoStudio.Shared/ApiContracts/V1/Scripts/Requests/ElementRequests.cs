namespace AiVideoStudio.Shared.ApiContracts.V1.Scripts.Requests;

public class UpdateSceneElementRequest
{
    public string? Content { get; set; }
    public string? Metadata { get; set; }
    public int ExpectedVersion { get; set; }
}
