using ElasticSync.Models;
using ElasticSync.NET.Interface;
using Nest;
using Npgsql;
using System.Text.Json;

namespace ElasticSync.Net.PostgreSql.Services
{
    public class PostgreChangeLogService : IChangeLogService
    {
       
        private readonly ElasticClient _elastic;
        private readonly ElasticSyncOptions _options;
        private readonly string _namingPrefix = "elastic_sync_";

        public PostgreChangeLogService(ElasticClient elastic, ElasticSyncOptions options)
        {
            _elastic = elastic;
            _options = options;
        }

        public virtual async Task<bool> ProcessChangeLogsAsync(string workerId, int batchSize, CancellationToken ct)
        {
            var logs = new List<ChangeLogEntry>(batchSize);

            try
            {
                while (true)
                {
                    logs = await FetchUnprocessedLogsAsync(workerId, batchSize, ct);
                    Console.WriteLine();
                    Console.WriteLine($"Worker {workerId} fetched {logs.Count} logs for processing...");
                    if (!logs.Any()) return false;

                    var bulk = new BulkDescriptor();
                    var logIdOrder = new List<int>();

                    foreach (var log in logs)
                    {
                        var entityConfig = _options.Entities.FirstOrDefault(e => e.Table.Equals(log.TableName, StringComparison.OrdinalIgnoreCase));
                        if (entityConfig == null) continue;

                        var entity = JsonSerializer.Deserialize(log.Payload, entityConfig.EntityType);
                        var entityId = entityConfig.EntityType.GetProperty(entityConfig.PrimaryKey)?.GetValue(entity)?.ToString();

                        if (string.IsNullOrWhiteSpace(entityId)) continue;

                        logIdOrder.Add(log.Id);

                        if (log.Operation == "DELETE")
                        {
                            bulk.Delete<dynamic>(op => op
                                .Index(entityConfig.IndexVersion is not null ? $"{entityConfig.IndexName}-{entityConfig.IndexVersion}" : entityConfig.IndexName ?? log.TableName)
                                .Id(entityId)
                            );
                        }
                        else
                        {
                            bulk.Index<object>(d => d
                                .Index(entityConfig.IndexVersion is not null ? $"{entityConfig.IndexName}-{entityConfig.IndexVersion}" : entityConfig.IndexName ?? log.TableName)
                                .Id(entityId)
                                .Document(entity)
                            );
                        }
                    }

                    var response = await _elastic.BulkAsync(bulk);
                    if (!response.ApiCall.Success)
                    {
                        Console.WriteLine($"ElasticSearch bulk operation failed: {response.ApiCall.OriginalException.ToString()}");
                        return false;
                    }

                    var successIds = new List<int>();
                    var failures = new List<(int, string)>();

                    for (int i = 0; i < response.Items.Count; i++)
                    {
                        var item = response.Items[i];
                        var logId = logIdOrder[i];

                        if (item.IsValid || (item.Status == 200 || item.Status == 201))
                            successIds.Add(logId);
                        else
                            failures.Add((logId, item.Error?.Reason ?? "Unknown error"));
                    }
                    await MarkLogsAsProcessed(successIds, ct);
                    await HandleFailedLogs(failures, ct);

                    Console.WriteLine($"Worker {workerId} processing finished");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return true;
        }

        public virtual async Task<List<ChangeLogEntry>> FetchUnprocessedLogsAsync(string workerId, int batchSize, CancellationToken cancellationToken)
        {
            var logs = new List<ChangeLogEntry>(batchSize);
            string sql;

            try
            {
                if (_options.IsMultipleWorkers)
                {
                    //If parallel processing is enabled, we lock the rows to prevent other workers from processing them
                    sql = string.Format(@"
                    WITH cte AS (
                        SELECT id
                        FROM esnet.{0}change_log
                        WHERE processed = FALSE
                            AND dead_letter = FALSE
                            AND (next_retry_at IS NULL OR next_retry_at <= now())
                            AND locked_by IS NULL
                        ORDER BY id
                        LIMIT {1}
                        FOR UPDATE SKIP LOCKED
                    )
                    UPDATE esnet.{0}change_log cl
                    SET processed_by = '{2}',
                        locked_by ='{2}',
                        locked_at = now()
                    FROM cte
                    WHERE cl.id = cte.id
                    RETURNING cl.id, cl.table_name, cl.operation, cl.record_id, cl.payload, cl.retry_count;", _namingPrefix, batchSize, workerId);
                }
                else
                {
                    // If parallel processing is not enabled, we simply fetch the logs without locking
                    sql = string.Format(@"
                    WITH cte AS (
                        SELECT id, table_name, operation, record_id, payload, retry_count
                        FROM esnet.{0}change_log
                        WHERE processed = FALSE
                            AND dead_letter = FALSE
                            AND (next_retry_at IS NULL OR next_retry_at <= now())
                        ORDER BY id
                        LIMIT {1}
                    )
                    SELECT id, table_name, operation, record_id, payload, retry_count
                    FROM cte;", _namingPrefix, batchSize);
                }

                await using var conn = new NpgsqlConnection(_options.ConnectionString);
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    logs.Add(new ChangeLogEntry
                    {
                        Id = reader.GetInt32(0),
                        TableName = reader.GetString(1),
                        Operation = reader.GetString(2),
                        RecordId = reader.GetString(3),
                        Payload = reader.GetString(4),
                        RetryCount = reader.GetInt32(5)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return logs;
        }

        public virtual async Task MarkLogsAsProcessed(List<int> successIds, CancellationToken cancellationToken)
        {
            try
            {
                if (!successIds.Any()) return;

                await using var conn = new NpgsqlConnection(_options.ConnectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand($@"
                UPDATE esnet.{_namingPrefix}change_log 
                SET processed = TRUE,
                    last_attempt_at = now(),
                    locked_by = NULL,
                    locked_at = NULL,
                    next_retry_at = NULL
                WHERE id = ANY(@ids)", conn);

                cmd.Parameters.AddWithValue("ids", successIds.ToArray());
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task HandleFailedLogs(List<(int logId, string error)> failures, CancellationToken cancellationToken)
        {
            if (!failures.Any()) return;

            try
            {
                await using var conn = new NpgsqlConnection(_options.ConnectionString);
                await conn.OpenAsync();

                foreach (var (logId, error) in failures)
                {
                    var cmd = new NpgsqlCommand($@"
                    UPDATE esnet.{_namingPrefix}change_log
                    SET retry_count = retry_count + 1,
                        locked_by = NULL,
                        last_attempt_at = now(),   
                        last_error = @error,
                        dead_letter = retry_count + 1 >= @maxRetries,
                        next_retry_at = now() + @retryDelayInSeconds
                    WHERE id = @id", conn);

                    cmd.Parameters.AddWithValue("error", error);
                    cmd.Parameters.AddWithValue("maxRetries", _options.MaxRetries);
                    cmd.Parameters.AddWithValue("retryDelayInSeconds", TimeSpan.FromSeconds(_options.RetryDelayInSeconds));
                    cmd.Parameters.AddWithValue("id", logId);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
