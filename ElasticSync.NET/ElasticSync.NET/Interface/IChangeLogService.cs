using ElasticSync.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ElasticSync.NET.Interface
{
    public interface IChangeLogService
    {
        Task<bool> ProcessChangeLogsAsync(string worderId, int batchSize, CancellationToken cancellationToken);
        //Task<IEnumerable<ChangeLogEntry>> GetPendingChangesAsync(int batchSize, CancellationToken ct);
        //Task MarkProcessedAsync(ChangeLogEntry entry, bool success, CancellationToken ct);
        //Task LockEntriesAsync(IEnumerable<int> ids, string lockedBy, CancellationToken ct);
    }
}
