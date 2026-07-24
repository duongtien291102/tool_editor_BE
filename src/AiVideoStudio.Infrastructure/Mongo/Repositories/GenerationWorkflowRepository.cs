using AiVideoStudio.Domain.Entities.Orchestration;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces.Orchestration;
using AiVideoStudio.Shared.Responses;
using MongoDB.Driver;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public sealed class GenerationWorkflowRepository : IGenerationWorkflowRepository
{
    private readonly IMongoCollection<GenerationWorkflow> _workflows;
    private readonly IMongoCollection<WorkflowHistory> _histories;

    public GenerationWorkflowRepository(IMongoCollection<GenerationWorkflow> workflows, IMongoCollection<WorkflowHistory> histories)
    {
        _workflows = workflows;
        _histories = histories;
    }

    public GenerationWorkflowRepository(MongoDbContext dbContext)
    {
        _workflows = dbContext.GenerationWorkflows;
        _histories = dbContext.WorkflowHistories;
    }

    public async Task<GenerationWorkflow?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _workflows.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(GenerationWorkflow workflow, CancellationToken ct = default)
    {
        await _workflows.InsertOneAsync(workflow, cancellationToken: ct);
        if (workflow.History.Count > 0)
        {
            await _histories.InsertManyAsync(workflow.History, cancellationToken: ct);
        }
    }

    public async Task UpdateAsync(GenerationWorkflow workflow, CancellationToken ct = default)
    {
        await _workflows.ReplaceOneAsync(x => x.Id == workflow.Id, workflow, cancellationToken: ct);
        var existingHistoryIds = (await _histories.Find(x => x.WorkflowId == workflow.Id).ToListAsync(ct))
            .Select(h => h.Id)
            .ToHashSet();

        var newHistories = workflow.History.Where(h => !existingHistoryIds.Contains(h.Id)).ToList();
        if (newHistories.Count > 0)
        {
            await _histories.InsertManyAsync(newHistories, cancellationToken: ct);
        }
    }

    public async Task<PagedResult<GenerationWorkflow>> GetByProjectAsync(string projectId, int page, int size, CancellationToken ct = default)
    {
        var filter = Builders<GenerationWorkflow>.Filter.Eq(x => x.ProjectId, projectId);
        var total = await _workflows.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _workflows.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * size)
            .Limit(size)
            .ToListAsync(ct);

        return new PagedResult<GenerationWorkflow>(items, (int)total, page, size);
    }

    public async Task<IReadOnlyList<GenerationWorkflow>> GetQueuedWorkflowsAsync(int batchSize = 10, CancellationToken ct = default)
    {
        var filter = Builders<GenerationWorkflow>.Filter.Eq(x => x.State, WorkflowState.Queued);
        var items = await _workflows.Find(filter)
            .SortBy(x => x.CreatedAt)
            .Limit(batchSize)
            .ToListAsync(ct);

        return items;
    }

    public async Task<IReadOnlyList<WorkflowHistory>> GetHistoryAsync(string workflowId, CancellationToken ct = default)
    {
        return await _histories.Find(x => x.WorkflowId == workflowId)
            .SortBy(x => x.Timestamp)
            .ToListAsync(ct);
    }

    public async Task AddHistoryAsync(WorkflowHistory history, CancellationToken ct = default)
    {
        await _histories.InsertOneAsync(history, cancellationToken: ct);
    }
}
