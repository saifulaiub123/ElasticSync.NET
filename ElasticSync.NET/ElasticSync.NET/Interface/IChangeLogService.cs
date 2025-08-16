using System.Threading;
using System.Threading.Tasks;

namespace ElasticSync.NET.Interface
{
    public interface IChangeLogService
    {
        Task<bool> ProcessChangeLogsAsync(string? worderId, int batchSize, CancellationToken cancellationToken);
    }
}
