using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Data.Seed;

public interface ISeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
