using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Data.Seed.Seeders;

public class UserSeeder : ISeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Scaffold only
        return Task.CompletedTask;
    }
}
