using AiVideoStudio.Shared.Configuration;
using Microsoft.Extensions.Configuration;

namespace AiVideoStudio.Infrastructure.Configuration;

public class AppConfiguration : IAppConfiguration
{
    private readonly IConfiguration _configuration;

    public AppConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString(string name)
    {
        return _configuration.GetConnectionString(name) ?? string.Empty;
    }

    public T GetSection<T>(string key) where T : new()
    {
        var obj = new T();
        _configuration.GetSection(key).Bind(obj);
        return obj;
    }

    public string GetSetting(string key)
    {
        return _configuration[key] ?? string.Empty;
    }
}
