namespace AiVideoStudio.Shared.Configuration;

public interface IAppConfiguration
{
    string GetConnectionString(string name);
    string GetSetting(string key);
    T GetSection<T>(string key) where T : new();
}
