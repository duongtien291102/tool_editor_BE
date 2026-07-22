using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Data.Seed.Seeders;

public class RoleSeeder : ISeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Scaffold only
        return Task.CompletedTask;
    }
}
