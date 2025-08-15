using ChangeSync.Elastic.Postgres.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ElasticSync.NET.Services.Interface
{
    public interface IElasticSyncNetService
    {
        Task<bool> ProcessChangeLogsAsync(string worderId, int batchSize, CancellationToken cancellationToken);
    }
}
