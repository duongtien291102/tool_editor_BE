using System;
using System.Collections.Generic;
using System.Linq;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Domain.Entities;

public class Scene
{
    public string Id { get; private set; }
    public string ScriptId { get; private set; }
    public string Name { get; private set; }
    public int Order { get; private set; }
    public TimeSpan Duration { get; private set; }
    public string? Notes { get; private set; }
    
    private readonly List<SceneElement> _elements = new();
    public IReadOnlyCollection<SceneElement> Elements => _elements.AsReadOnly();

    protected Scene() 
    {
        Id = Guid.NewGuid().ToString("N");
        ScriptId = string.Empty;
        Name = string.Empty;
    }

    internal Scene(string scriptId, string name, int order, TimeSpan duration, string? notes = null)
    {
        Id = Guid.NewGuid().ToString("N");
        ScriptId = scriptId;
        Name = name;
        Order = order;
        Duration = duration;
        Notes = notes;
    }

    internal void Update(string name, TimeSpan duration, string? notes)
    {
        Name = name;
        Duration = duration;
        Notes = notes;
    }

    internal void SetOrder(int order)
    {
        Order = order;
    }

    internal SceneElement AddElement(ElementType elementType, string? content = null, string? metadata = null)
    {
        var order = _elements.Count > 0 ? _elements.Max(e => e.Order) + 1 : 1;
        var element = new SceneElement(Id, order, elementType, content, metadata);
        _elements.Add(element);
        return element;
    }

    internal void RemoveElement(string elementId)
    {
        var element = _elements.FirstOrDefault(e => e.Id == elementId);
        if (element != null)
        {
            _elements.Remove(element);
            // Re-adjust order
            var orderedElements = _elements.OrderBy(e => e.Order).ToList();
            for (int i = 0; i < orderedElements.Count; i++)
            {
                orderedElements[i].SetOrder(i + 1);
            }
        }
    }

    internal void MoveElement(string elementId, int newOrder)
    {
        var element = _elements.FirstOrDefault(e => e.Id == elementId);
        if (element == null) return;

        if (newOrder < 1) newOrder = 1;
        if (newOrder > _elements.Count) newOrder = _elements.Count;

        var currentOrder = element.Order;
        if (currentOrder == newOrder) return;

        if (newOrder > currentOrder)
        {
            foreach (var e in _elements.Where(x => x.Order > currentOrder && x.Order <= newOrder))
            {
                e.SetOrder(e.Order - 1);
            }
        }
        else
        {
            foreach (var e in _elements.Where(x => x.Order >= newOrder && x.Order < currentOrder))
            {
                e.SetOrder(e.Order + 1);
            }
        }

        element.SetOrder(newOrder);
    }
}
