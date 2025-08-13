using ChangeSync.Elastic.Postgres.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElasticSync.NET.Services.Interface
{
    public interface IElasticSyncNetService
    {
        Task<List<ChangeLogEntry>> FetchUnprocessedLogsAsync();
        Task ProcessChangeLogsAsync();
        Task MarkLogsAsProcessed(List<int> successIds);
    }
}
