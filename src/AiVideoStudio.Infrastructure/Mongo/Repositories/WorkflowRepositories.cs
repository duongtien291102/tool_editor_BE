using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.Responses;
using MongoDB.Driver;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public sealed class AIWorkflowRepository:IAIWorkflowRepository
{
    private readonly IMongoCollection<AIWorkflow> _items;public AIWorkflowRepository(MongoDbContext context):this(context.AIWorkflows){}public AIWorkflowRepository(IMongoCollection<AIWorkflow> items)=>_items=items;
    public Task<AIWorkflow?> GetByIdAsync(string id,CancellationToken ct=default)=>_items.Find(x=>x.Id==id&&x.DeletedAt==null).FirstOrDefaultAsync(ct)!;
    public Task AddAsync(AIWorkflow workflow,CancellationToken ct=default)=>_items.InsertOneAsync(workflow,cancellationToken:ct);
    public async Task UpdateAsync(AIWorkflow workflow,CancellationToken ct=default)=>await _items.ReplaceOneAsync(x=>x.Id==workflow.Id,workflow,cancellationToken:ct);
    public async Task<PagedResult<AIWorkflow>> GetByProjectAsync(string projectId,int page,int size,CancellationToken ct=default){var filter=Builders<AIWorkflow>.Filter.And(Builders<AIWorkflow>.Filter.Eq(x=>x.ProjectId,projectId),Builders<AIWorkflow>.Filter.Eq(x=>x.DeletedAt,null));var total=await _items.CountDocumentsAsync(filter,cancellationToken:ct);var data=await _items.Find(filter).SortByDescending(x=>x.CreatedAt).Skip((page-1)*size).Limit(size).ToListAsync(ct);return new(data,(int)total,page,size);}
}

public sealed class WorkflowExecutionRepository:IWorkflowExecutionRepository
{
    private readonly IMongoCollection<WorkflowExecution> _items;public WorkflowExecutionRepository(MongoDbContext context):this(context.WorkflowExecutions){}public WorkflowExecutionRepository(IMongoCollection<WorkflowExecution> items)=>_items=items;
    public Task<WorkflowExecution?> GetByIdAsync(string id,CancellationToken ct=default)=>_items.Find(x=>x.Id==id).FirstOrDefaultAsync(ct)!;
    public Task<WorkflowExecution?> GetLatestAsync(string workflowId,CancellationToken ct=default)=>_items.Find(x=>x.WorkflowId==workflowId).SortByDescending(x=>x.StartedAt).FirstOrDefaultAsync(ct)!;
    public Task AddAsync(WorkflowExecution execution,CancellationToken ct=default)=>_items.InsertOneAsync(execution,cancellationToken:ct);
    public async Task UpdateAsync(WorkflowExecution execution,CancellationToken ct=default)=>await _items.ReplaceOneAsync(x=>x.Id==execution.Id,execution,cancellationToken:ct);
}
