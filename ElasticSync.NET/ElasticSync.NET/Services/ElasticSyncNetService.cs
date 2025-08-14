using ChangeSync.Elastic.Postgres.Models;
using ElasticSync.NET.Services.Interface;
using Nest;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ElasticSync.NET.Services
{
    public class ElasticSyncNetService : IElasticSyncNetService
    {
        private readonly ElasticClient _elastic;
        private readonly ChangeSyncOptions _options;
        private readonly string _namingPrefix = "elastic_sync_";

        public ElasticSyncNetService(ElasticClient elastic, ChangeSyncOptions options)
        {
            _elastic = elastic;
            _options = options;
        }

        public virtual async Task<List<ChangeLogEntry>> FetchUnprocessedLogsAsync(int batchSize, CancellationToken cancellationToken)
        {
            var logs = new List<ChangeLogEntry>(_options.BatchSize);

            await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
            SELECT id, table_name, operation, record_id, payload, retry_count
            FROM esnet.{_namingPrefix}change_log
            WHERE processed = FALSE AND dead_letter = FALSE
            ORDER BY id
            LIMIT {_options.BatchSize}", conn);

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
            return logs;
        }

        public virtual async Task MarkLogsAsProcessed(List<int> successIds, CancellationToken cancellationToken)
        {
            if (!successIds.Any()) return;

            await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($"UPDATE esnet.{_namingPrefix}change_log SET processed = TRUE WHERE id = ANY(@ids)", conn);
            cmd.Parameters.AddWithValue("ids", successIds.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }

        public virtual async Task ProcessChangeLogsAsync(string worderId, int batchSize, CancellationToken cancellationToken)
        {
            var logs = new List<ChangeLogEntry>(batchSize);

            while (true) 
            {
                logs = await FetchUnprocessedLogsAsync(batchSize, cancellationToken);
                if (!logs.Any()) break;


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
                await MarkLogsAsProcessed(successIds, cancellationToken);
                await HandleFailedLogs(failures, cancellationToken);
            }
        }

        private async Task HandleFailedLogs(List<(int logId, string error)> failures, CancellationToken cancellationToken)
        {
            if (!failures.Any()) return;

            await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
            await conn.OpenAsync();

            foreach (var (logId, error) in failures)
            {
                var cmd = new NpgsqlCommand($@"
                UPDATE esnet.{_namingPrefix}change_log
                SET retry_count = retry_count + 1,
                    last_error = @error,
                    dead_letter = retry_count + 1 >= @maxRetries
                WHERE id = @id", conn);

                cmd.Parameters.AddWithValue("error", error);
                cmd.Parameters.AddWithValue("id", logId);
                cmd.Parameters.AddWithValue("maxRetries", _options.MaxRetries);

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
