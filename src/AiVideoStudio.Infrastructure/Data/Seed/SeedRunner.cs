using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Data.Seed;

public class SeedRunner
{
    private readonly IEnumerable<ISeeder> _seeders;

    public SeedRunner(IEnumerable<ISeeder> seeders)
    {
        _seeders = seeders;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        foreach (var seeder in _seeders)
        {
            await seeder.SeedAsync(cancellationToken);
        }
    }
}
