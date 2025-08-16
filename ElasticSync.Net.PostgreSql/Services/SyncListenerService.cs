using Npgsql;
using ElasticSync.NET.Models;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;
using ElasticSync.NET.Interface;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System;

namespace ElasticSync.Net.PostgreSql.Services;

public class SyncListenerService : BackgroundService, ISyncListenerHostedService
{
    private readonly ElasticSyncOptions _options;
    private readonly Channel<byte> _notifyChannel;
    private readonly IChangeLogService _elasticSyncNetService;

    private readonly string _namingPrefix = "elastic_sync_";
    private int Count { get; set; } = 0;

    public SyncListenerService(ElasticSyncOptions options, IChangeLogService elasticSyncNetService)
    {
        _options = options;
        _elasticSyncNetService = elasticSyncNetService;
        _notifyChannel = Channel.CreateBounded<byte>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var noOfWorkers = _options.IsMultipleWorkers ? _options.WorkerOptions.NumberOfWorkers : 1;
            var batchSize = _options.IsMultipleWorkers ? _options.WorkerOptions.BatchSizePerWorker : _options.BatchSize;

            var workers = Enumerable.Range(0, noOfWorkers)
                .Select(i =>
                    Task.Run(() =>
                        WorkerLoopParallelAsync(string.Format("worker_{0}", i + 1), batchSize, ct), ct)
                    )
                .ToList();

            if (_options.IsRealTimeSync)
            {
                var listenerTask = Task.Run(() => ListenToPgNotifyParallelAsync(ct), ct);
                await Task.WhenAll(workers.Concat(new[] { listenerTask })); 
            }
            else if (_options.IsIntervalSync)
            {
                var intervalFallbackListner = Task.Run(() => IntervalFallbackListener(ct), ct);
                await Task.WhenAll(workers.Concat(new[] { intervalFallbackListner }));
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    private async Task WorkerLoopParallelAsync(string workerId, int batchSize, CancellationToken ct)
    {
        // each worker has its own DB connection
        try
        {
            await using var conn = new NpgsqlConnection(_options.ConnectionString);
            await conn.OpenAsync(ct);

            while (!ct.IsCancellationRequested)
            {
                // Wait for a notification token
                try
                {
                    await _notifyChannel.Reader.ReadAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                // Process until no more batches (to drain quickly)
                while (!ct.IsCancellationRequested)
                {
                    var hasWork = await _elasticSyncNetService.ProcessChangeLogsAsync(workerId, batchSize, ct);
                    if (!hasWork)
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task ListenToPgNotifyParallelAsync(CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync(ct);

        conn.Notification += (o, e) =>
        {
            for (int i = 0; i < _options.WorkerOptions.NumberOfWorkers; i++)
            {
                _ = _notifyChannel.Writer.WriteAsync(1);
            }
        };

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"LISTEN {_namingPrefix}change_log_channel;";
        await cmd.ExecuteNonQueryAsync();

        // Wait loop to keep connection alive and receive notifications
        while (!ct.IsCancellationRequested)
        {
            // Npgsql exposes WaitAsync to wait for notifications
            try
            {
                await conn.WaitAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                // Log and try to reconnect
                Console.WriteLine($"[Listener] error: {ex.Message}");
                await Task.Delay(1000, ct);
            }
        }
    }

    private async Task IntervalFallbackListener(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.IntervalInSeconds), ct);
            await _notifyChannel.Writer.WriteAsync(1, ct);
        }
    }
}