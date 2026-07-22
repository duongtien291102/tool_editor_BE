using System;

namespace AiVideoStudio.Shared.ApiContracts.V1.Scripts.Requests;

public class AddSceneRequest
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string? Notes { get; set; }
    public int ExpectedVersion { get; set; }
}

public class UpdateSceneRequest
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string? Notes { get; set; }
    public int ExpectedVersion { get; set; }
}

public class ReorderSceneRequest
{
    public string SceneId { get; set; } = string.Empty;
    public int NewOrder { get; set; }
    public int ExpectedVersion { get; set; }
}
