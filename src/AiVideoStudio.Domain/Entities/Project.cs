using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Projects;
using System;

namespace AiVideoStudio.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Thumbnail { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
    public bool IsDeleted => DeletedAt.HasValue;

    public Project()
    {
    }

    public static Project Create(string name, string ownerId, string? description = null, string? thumbnail = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name cannot be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException("Project owner ID cannot be empty.", nameof(ownerId));
        }

        var project = new Project
        {
            Name = name.Trim(),
            OwnerId = ownerId,
            Description = description?.Trim(),
            Thumbnail = thumbnail?.Trim(),
            Status = ProjectStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = ownerId
        };

        project.AddDomainEvent(new ProjectCreatedEvent(project.Id, ownerId));
        return project;
    }

    public void Update(string name, string? description, string? thumbnail, ProjectStatus? status, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
        Description = description?.Trim();
        Thumbnail = thumbnail?.Trim();

        if (status.HasValue)
        {
            Status = status.Value;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new ProjectUpdatedEvent(Id, updatedBy));
    }

    public void SoftDelete(string deletedBy)
    {
        if (IsDeleted)
            return;

        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;

        AddDomainEvent(new ProjectDeletedEvent(Id, deletedBy));
    }
}
