using System.Text.Json;
using Npgsql;
using Nest;
using ChangeSync.Elastic.Postgres.Models;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ChangeSync.Elastic.Postgres.Services;

public class ChangeLogListenerService : BackgroundService
{
    private readonly ElasticClient _elastic;
    private readonly ChangeSyncOptions _options;
    private readonly string _namingPrefix = "elastic_sync_";

    public ChangeLogListenerService(ElasticClient elastic, ChangeSyncOptions options)
    {
        _elastic = elastic;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_options.Mode == ElasticSyncMode.Realtime)
            {
                await ListenToPgNotifyAsync(cancellationToken);
            }
            else if (_options.Mode == ElasticSyncMode.Interval)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), cancellationToken);
                    await ProcessChangeLogsAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
    private async Task ListenToPgNotifyAsync(CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"LISTEN {_namingPrefix}change_log_channel;";
        await cmd.ExecuteNonQueryAsync();

        while (!cancellationToken.IsCancellationRequested)
        {
            await conn.WaitAsync(cancellationToken);
            await ProcessChangeLogsAsync();
        }
    }

    private async Task ProcessChangeLogsAsync()
    {
        var logs = await FetchUnprocessedLogsAsync();
        if (!logs.Any()) return;

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
        await MarkLogsAsProcessed(successIds);
        await HandleFailedLogs(failures);
    }

    private async Task<List<ChangeLogEntry>> FetchUnprocessedLogsAsync()
    {
        var logs = new List<ChangeLogEntry>();

        await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand($@"
            SELECT id, table_name, operation, record_id, payload, retry_count
            FROM {_namingPrefix}change_log
            WHERE processed = FALSE AND dead_letter = FALSE
            ORDER BY id
            LIMIT 100", conn);

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

    private async Task MarkLogsAsProcessed(List<int> successIds)
    {
        if (!successIds.Any()) return;

        await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand($"UPDATE {_namingPrefix}change_log SET processed = TRUE WHERE id = ANY(@ids)", conn);
        cmd.Parameters.AddWithValue("ids", successIds.ToArray());
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task HandleFailedLogs(List<(int logId, string error)> failures)
    {
        if (!failures.Any()) return;

        await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
        await conn.OpenAsync();

        foreach (var (logId, error) in failures)
        {
            var cmd = new NpgsqlCommand($@"
                UPDATE {_namingPrefix}change_log
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