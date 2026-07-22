using System;
using System.Collections.Generic;
using System.Linq;
using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Scripts;

namespace AiVideoStudio.Domain.Entities;

public class Script : BaseEntity
{
    public string ProjectId { get; private set; } = string.Empty;
    public string OwnerId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Version { get; private set; }
    
    public bool IsDeleted => DeletedAt.HasValue;

    private List<Scene> _scenes = new();
    public IReadOnlyCollection<Scene> Scenes => _scenes.AsReadOnly();

    protected Script() 
    {
    }

    public static Script Create(string projectId, string ownerId, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Script name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID cannot be empty.", nameof(projectId));
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));

        var script = new Script
        {
            ProjectId = projectId,
            OwnerId = ownerId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = ownerId
        };

        script.AddDomainEvent(new ScriptCreatedEvent(script.Id, script.ProjectId, script.OwnerId));
        return script;
    }

    private void IncrementVersion(string updatedBy)
    {
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Rename(string name, string? description, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Script name cannot be empty.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        
        IncrementVersion(updatedBy);
        AddDomainEvent(new ScriptUpdatedEvent(Id, updatedBy));
    }

    public void SoftDelete(string deletedBy)
    {
        if (IsDeleted) return;

        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
        IncrementVersion(deletedBy);
        
        AddDomainEvent(new ScriptDeletedEvent(Id, deletedBy));
    }

    public Scene AddScene(string name, TimeSpan duration, string? notes, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Scene name cannot be empty.", nameof(name));

        var order = _scenes.Count > 0 ? _scenes.Max(s => s.Order) + 1 : 1;
        var scene = new Scene(Id, name, order, duration, notes);
        _scenes.Add(scene);
        
        IncrementVersion(updatedBy);
        AddDomainEvent(new SceneAddedEvent(Id, scene.Id));
        
        return scene;
    }

    public void RemoveScene(string sceneId, string updatedBy)
    {
        var scene = _scenes.FirstOrDefault(s => s.Id == sceneId);
        if (scene != null)
        {
            _scenes.Remove(scene);
            
            // Re-adjust order
            var orderedScenes = _scenes.OrderBy(s => s.Order).ToList();
            for (int i = 0; i < orderedScenes.Count; i++)
            {
                orderedScenes[i].SetOrder(i + 1);
            }

            IncrementVersion(updatedBy);
            AddDomainEvent(new SceneRemovedEvent(Id, sceneId));
        }
    }

    public void ReorderScene(string sceneId, int newOrder, string updatedBy)
    {
        var scene = _scenes.FirstOrDefault(s => s.Id == sceneId);
        if (scene == null) return;

        if (newOrder < 1) newOrder = 1;
        if (newOrder > _scenes.Count) newOrder = _scenes.Count;

        var currentOrder = scene.Order;
        if (currentOrder == newOrder) return;

        if (newOrder > currentOrder)
        {
            foreach (var s in _scenes.Where(x => x.Order > currentOrder && x.Order <= newOrder))
            {
                s.SetOrder(s.Order - 1);
            }
        }
        else
        {
            foreach (var s in _scenes.Where(x => x.Order >= newOrder && x.Order < currentOrder))
            {
                s.SetOrder(s.Order + 1);
            }
        }

        scene.SetOrder(newOrder);
        
        IncrementVersion(updatedBy);
        AddDomainEvent(new ScriptUpdatedEvent(Id, updatedBy)); // General script update event
    }

    public void UpdateScene(string sceneId, string name, TimeSpan duration, string? notes, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Scene name cannot be empty.", nameof(name));

        var scene = _scenes.FirstOrDefault(s => s.Id == sceneId);
        if (scene != null)
        {
            scene.Update(name, duration, notes);
            IncrementVersion(updatedBy);
            AddDomainEvent(new SceneUpdatedEvent(Id, sceneId));
        }
    }

    public SceneElement AddSceneElement(string sceneId, ElementType elementType, string? content, string? metadata, string updatedBy)
    {
        var scene = _scenes.FirstOrDefault(s => s.Id == sceneId);
        if (scene == null)
            throw new InvalidOperationException($"Scene with ID {sceneId} not found in this script.");

        var element = scene.AddElement(elementType, content, metadata);
        IncrementVersion(updatedBy);
        AddDomainEvent(new SceneUpdatedEvent(Id, sceneId));
        return element;
    }

    public void RemoveSceneElement(string sceneId, string elementId, string updatedBy)
    {
        var scene = _scenes.FirstOrDefault(s => s.Id == sceneId);
        if (scene != null)
        {
            scene.RemoveElement(elementId);
            IncrementVersion(updatedBy);
            AddDomainEvent(new SceneUpdatedEvent(Id, sceneId));
        }
    }

    public void MoveSceneElement(string sceneId, string elementId, int newOrder, string updatedBy)
    {
        var scene = _scenes.FirstOrDefault(s => s.Id == sceneId);
        if (scene != null)
        {
            scene.MoveElement(elementId, newOrder);
            IncrementVersion(updatedBy);
            AddDomainEvent(new SceneUpdatedEvent(Id, sceneId));
        }
    }
    
    public void UpdateSceneElement(string sceneId, string elementId, string? content, string? metadata, string updatedBy)
    {
        var scene = _scenes.FirstOrDefault(s => s.Id == sceneId);
        if (scene != null)
        {
            var element = scene.Elements.FirstOrDefault(e => e.Id == elementId);
            if (element != null)
            {
                element.UpdateContent(content);
                element.UpdateMetadata(metadata);
                IncrementVersion(updatedBy);
                AddDomainEvent(new SceneElementUpdatedEvent(Id, sceneId, elementId));
            }
        }
    }

    // Explicit method for AutoSave where the handler manually applies targeted updates 
    // to nested collections/fields and calls this to bump version.
    public void ForceIncrementVersionForAutoSave(string updatedBy)
    {
        IncrementVersion(updatedBy);
        AddDomainEvent(new ScriptUpdatedEvent(Id, updatedBy));
    }
}
