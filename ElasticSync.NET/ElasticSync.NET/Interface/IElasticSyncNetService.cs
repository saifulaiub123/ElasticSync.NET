using ElasticSync.NET.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ElasticSync.NET.Interface
{
    public interface IElasticSyncNetService
    {
        Task<bool> ProcessChangeLogsAsync(string worderId, int batchSize, CancellationToken cancellationToken);
    }
}
