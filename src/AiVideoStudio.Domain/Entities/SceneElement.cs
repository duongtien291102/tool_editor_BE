using System;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Domain.Entities;

public class SceneElement
{
    public string Id { get; private set; }
    public string SceneId { get; private set; }
    public int Order { get; private set; }
    public ElementType ElementType { get; private set; }
    public string? Content { get; private set; }
    public string? Metadata { get; private set; }

    // For EF/MongoDB mapping
    protected SceneElement() 
    {
        Id = Guid.NewGuid().ToString("N");
        SceneId = string.Empty;
    }

    internal SceneElement(string sceneId, int order, ElementType elementType, string? content = null, string? metadata = null)
    {
        Id = Guid.NewGuid().ToString("N");
        SceneId = sceneId;
        Order = order;
        ElementType = elementType;
        Content = content;
        Metadata = metadata;
    }

    internal void UpdateContent(string? content)
    {
        Content = content;
    }

    internal void UpdateMetadata(string? metadata)
    {
        Metadata = metadata;
    }
    
    internal void SetOrder(int order)
    {
        Order = order;
    }
}
