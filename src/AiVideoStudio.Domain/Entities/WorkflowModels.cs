using AiVideoStudio.Domain.Enums;
namespace AiVideoStudio.Domain.Entities;
public sealed class WorkflowVariable { public string Name {get;private set;}=string.Empty;public string Value{get;private set;}=string.Empty;private WorkflowVariable(){}public WorkflowVariable(string name,string value){if(string.IsNullOrWhiteSpace(name))throw new ArgumentException(nameof(name));Name=name;Value=value;}public void Set(string value)=>Value=value; }
public sealed class WorkflowTemplate { public string Id{get;private set;}=Guid.NewGuid().ToString();public string Name{get;private set;}=string.Empty;public IReadOnlyCollection<WorkflowStep> Steps{get;private set;}=Array.Empty<WorkflowStep>();private WorkflowTemplate(){}public WorkflowTemplate(string name,IReadOnlyCollection<WorkflowStep> steps){Name=name;Steps=steps;} }
public sealed class WorkflowTrigger { public string Id{get;private set;}=Guid.NewGuid().ToString();public string Type{get;private set;}="Manual";public bool Enabled{get;private set;}=true;private WorkflowTrigger(){}public WorkflowTrigger(string type){Type=type;} }
public sealed class WorkflowStep
{
    public string Id{get;private set;}=Guid.NewGuid().ToString();public string Name{get;private set;}=string.Empty;public WorkflowStepType Type{get;private set;}
    public WorkflowStepStatus Status{get;private set;}=WorkflowStepStatus.Pending;public List<string> Dependencies{get;private set;}=new();
    public string? Condition{get;private set;}public int TimeoutSeconds{get;private set;}=60;public int MaxRetries{get;private set;}=1;public int RetryCount{get;private set;}
    public Dictionary<string,string> InputContext{get;private set;}=new();public Dictionary<string,string> OutputContext{get;private set;}=new();public string? Error{get;private set;}
    private WorkflowStep(){}public WorkflowStep(string name,WorkflowStepType type,IEnumerable<string>? dependencies=null,string? condition=null,int timeoutSeconds=60,int maxRetries=1,IDictionary<string,string>? inputs=null,string? id=null)
    {if(string.IsNullOrWhiteSpace(name))throw new ArgumentException(nameof(name));if(timeoutSeconds<=0||maxRetries<0)throw new ArgumentOutOfRangeException();Id=string.IsNullOrWhiteSpace(id)?Guid.NewGuid().ToString():id;Name=name;Type=type;Dependencies=dependencies?.Distinct().ToList()??new();Condition=condition;TimeoutSeconds=timeoutSeconds;MaxRetries=maxRetries;InputContext=inputs is null?new():new(inputs);}
    public void Wait()=>Status=WorkflowStepStatus.Waiting;public void Start(){if(Status is not(WorkflowStepStatus.Pending or WorkflowStepStatus.Waiting))throw new InvalidOperationException();Status=WorkflowStepStatus.Running;Error=null;}
    public void Complete(IDictionary<string,string>? output=null){if(Status!=WorkflowStepStatus.Running)throw new InvalidOperationException();Status=WorkflowStepStatus.Completed;OutputContext=output is null?new():new(output);}
    public void Fail(string error){if(Status!=WorkflowStepStatus.Running)throw new InvalidOperationException();Status=WorkflowStepStatus.Failed;Error=error;}
    public bool Retry(){if(Status!=WorkflowStepStatus.Failed||RetryCount>=MaxRetries)return false;RetryCount++;Status=WorkflowStepStatus.Pending;Error=null;return true;}
    public void Skip(){if(Status is WorkflowStepStatus.Completed or WorkflowStepStatus.Running)throw new InvalidOperationException();Status=WorkflowStepStatus.Skipped;}
    public void Cancel(){if(Status is not(WorkflowStepStatus.Completed or WorkflowStepStatus.Skipped))Status=WorkflowStepStatus.Cancelled;}
    public void Reset(){Status=WorkflowStepStatus.Pending;RetryCount=0;Error=null;OutputContext.Clear();}
}
