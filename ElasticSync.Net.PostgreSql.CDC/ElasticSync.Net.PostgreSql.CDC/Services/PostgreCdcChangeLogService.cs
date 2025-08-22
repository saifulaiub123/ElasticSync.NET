using ElasticSync.NET.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSync.Net.PostgreSql.CDC.Services
{
    public class PostgreCdcChangeLogService : IChangeLogService
    {
        public Task<bool> ProcessChangeLogsAsync(string? worderId, int batchSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
