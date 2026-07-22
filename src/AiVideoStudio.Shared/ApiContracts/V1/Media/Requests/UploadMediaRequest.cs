using System.ComponentModel.DataAnnotations;

namespace AiVideoStudio.Shared.ApiContracts.V1.Media.Requests;

public class UploadMediaRequest
{
    [Required]
    public string ProjectId { get; set; } = string.Empty;
}
