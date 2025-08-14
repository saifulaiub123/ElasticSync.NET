using ChangeSync.Elastic.Postgres.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ElasticSync.NET.Services.Interface
{
    public interface IElasticSyncNetService
    {
        Task<List<ChangeLogEntry>> FetchUnprocessedLogsAsync(int batchSize, CancellationToken cancellationToken);
        Task ProcessChangeLogsAsync(string worderId, int batchSize, CancellationToken cancellationToken);
        Task MarkLogsAsProcessed(List<int> successIds, CancellationToken cancellationToken);
    }
}
