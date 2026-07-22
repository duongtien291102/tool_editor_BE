using System.Collections.Concurrent;
using AiVideoStudio.Domain.Interfaces;

namespace AiVideoStudio.Infrastructure.Workflow;

public sealed class InMemoryWorkflowScheduler:IWorkflowScheduler
{
    private readonly ConcurrentQueue<string> _queue=new();private readonly ConcurrentDictionary<string,byte> _scheduled=new();private readonly SemaphoreSlim _signal=new(0);
    public ValueTask ScheduleAsync(string id,CancellationToken ct=default){ct.ThrowIfCancellationRequested();if(_scheduled.TryAdd(id,0)){_queue.Enqueue(id);_signal.Release();}return ValueTask.CompletedTask;}
    public async ValueTask<string> DequeueAsync(CancellationToken ct=default){while(true){await _signal.WaitAsync(ct);if(_queue.TryDequeue(out var id)&&_scheduled.TryRemove(id,out _))return id;}}
    public bool Remove(string id)=>_scheduled.TryRemove(id,out _);
}
