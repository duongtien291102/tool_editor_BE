namespace AiVideoStudio.Domain.Entities.Orchestration;

public sealed class WorkflowExecutionContext
{
    private readonly Dictionary<string, string> _context = new();

    public IReadOnlyDictionary<string, string> Data => _context.AsReadOnly();

    public WorkflowExecutionContext() { }

    public WorkflowExecutionContext(IDictionary<string, string>? initialData)
    {
        if (initialData is not null)
        {
            foreach (var kvp in initialData)
            {
                _context[kvp.Key] = kvp.Value;
            }
        }
    }

    public void Set(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _context[key] = value ?? string.Empty;
    }

    public string? Get(string key)
    {
        return _context.TryGetValue(key, out var val) ? val : null;
    }

    public bool ContainsKey(string key) => _context.ContainsKey(key);

    public void Merge(IDictionary<string, string> items)
    {
        foreach (var pair in items)
        {
            _context[pair.Key] = pair.Value;
        }
    }
}
