using System.ComponentModel.DataAnnotations;

namespace AiVideoStudio.Infrastructure.Configuration;

public class MongoOptions
{
    public const string SectionName = "MongoDb";

    private string _connectionString = string.Empty;
    private string _databaseName = string.Empty;

    [Required]
    public string ConnectionString
    {
        get
        {
            var envVal = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(envVal)) return envVal;

            return !string.IsNullOrWhiteSpace(_connectionString) && !_connectionString.StartsWith("${")
                ? _connectionString
                : "mongodb://localhost:27017";
        }
        set => _connectionString = value;
    }

    [Required]
    public string DatabaseName
    {
        get
        {
            var envVal = Environment.GetEnvironmentVariable("MONGODB_DATABASE");
            if (!string.IsNullOrWhiteSpace(envVal)) return envVal;

            return !string.IsNullOrWhiteSpace(_databaseName) && !_databaseName.StartsWith("${")
                ? _databaseName
                : "AiVideoStudio";
        }
        set => _databaseName = value;
    }
}
