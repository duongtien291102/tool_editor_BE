using MongoDB.Driver;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace AiVideoStudio.Infrastructure.Mongo;

public class MongoDbContext
{
    public IMongoClient Client { get; }
    public IMongoDatabase Database { get; }

    public MongoDbContext(IOptions<MongoOptions> options)
    {
        Client = new MongoClient(options.Value.ConnectionString);
        Database = Client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<User> Users => Database.GetCollection<User>("users");
    public IMongoCollection<Role> Roles => Database.GetCollection<Role>("roles");
    public IMongoCollection<Permission> Permissions => Database.GetCollection<Permission>("permissions");
    public IMongoCollection<RefreshToken> RefreshTokens => Database.GetCollection<RefreshToken>("refreshTokens");

    public IMongoCollection<Workspace> Workspaces => Database.GetCollection<Workspace>("workspaces");
    public IMongoCollection<Project> Projects => Database.GetCollection<Project>("projects");
    public IMongoCollection<Script> Scripts => Database.GetCollection<Script>("scripts");
    public IMongoCollection<Timeline> Timelines => Database.GetCollection<Timeline>("timelines");
    public IMongoCollection<Asset> Assets => Database.GetCollection<Asset>("assets");
    public IMongoCollection<MediaAsset> MediaAssets => Database.GetCollection<MediaAsset>("mediaAssets");
    public IMongoCollection<Job> Jobs => Database.GetCollection<Job>("jobs");
    public IMongoCollection<RenderJob> RenderJobs => Database.GetCollection<RenderJob>("renderJobs");
    public IMongoCollection<ExportJob> ExportJobs => Database.GetCollection<ExportJob>("exportJobs");
    public IMongoCollection<UploadSession> UploadSessions => Database.GetCollection<UploadSession>("uploadSessions");
    public IMongoCollection<Setting> Settings => Database.GetCollection<Setting>("settings");
}
