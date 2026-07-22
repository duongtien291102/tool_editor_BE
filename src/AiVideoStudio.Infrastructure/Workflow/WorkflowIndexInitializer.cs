using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Infrastructure.Mongo;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace AiVideoStudio.Infrastructure.Workflow;

public sealed class WorkflowIndexInitializer:IHostedService
{
    private readonly MongoDbContext _context;public WorkflowIndexInitializer(MongoDbContext context)=>_context=context;
    public async Task StartAsync(CancellationToken ct)
    {
        var workflowIndexes=new[]{
            new CreateIndexModel<AIWorkflow>(Builders<AIWorkflow>.IndexKeys.Ascending(x=>x.ProjectId)),
            new CreateIndexModel<AIWorkflow>(Builders<AIWorkflow>.IndexKeys.Ascending(x=>x.OwnerId)),
            new CreateIndexModel<AIWorkflow>(Builders<AIWorkflow>.IndexKeys.Ascending(x=>x.Status)),
            new CreateIndexModel<AIWorkflow>(Builders<AIWorkflow>.IndexKeys.Descending(x=>x.CreatedAt)),
            new CreateIndexModel<AIWorkflow>(Builders<AIWorkflow>.IndexKeys.Descending(x=>x.UpdatedAt))};
        await _context.AIWorkflows.Indexes.CreateManyAsync(workflowIndexes,ct);
        await _context.WorkflowExecutions.Indexes.CreateManyAsync(new[]{new CreateIndexModel<WorkflowExecution>(Builders<WorkflowExecution>.IndexKeys.Ascending(x=>x.WorkflowId).Descending(x=>x.StartedAt)),new CreateIndexModel<WorkflowExecution>(Builders<WorkflowExecution>.IndexKeys.Ascending(x=>x.Status))},ct);
    }
    public Task StopAsync(CancellationToken ct)=>Task.CompletedTask;
}
