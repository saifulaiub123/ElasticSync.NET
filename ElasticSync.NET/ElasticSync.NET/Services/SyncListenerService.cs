using Npgsql;
using Nest;
using ChangeSync.Elastic.Postgres.Models;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System;
using ElasticSync.NET.Services.Interface;
using System.Threading.Channels;
using System.Linq;
using System.Collections.Generic;

namespace ChangeSync.Elastic.Postgres.Services;

public class SyncListenerService : BackgroundService
{
    private readonly ElasticClient _elastic;
    private readonly ChangeSyncOptions _options;
    private readonly Channel<byte> _notifyChannel;
    private readonly IElasticSyncNetService _elasticSyncNetService;

    private readonly string _namingPrefix = "elastic_sync_";
    private int Count { get; set; } = 0;

    public SyncListenerService(ElasticClient elastic, ChangeSyncOptions options, IElasticSyncNetService elasticSyncNetService)
    {
        _elastic = elastic;
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
            if (_options.EnableParallelProcessing)
            {
                //var workers = new List<Task>(_options.WorkerOptions.NumberOfWorkers);
                // Start worker pool
                var workers = Enumerable.Range(0, _options.WorkerOptions.NumberOfWorkers)
                .Select(i => 
                    Task.Run(() => 
                        WorkerLoopParallelAsync(string.Format("worker_{0}", i + 1), _options.WorkerOptions.BatchSizePerWorker,ct), ct)
                    )
                .ToList();

                // Start listener loop
                var listenerTask = Task.Run(() => ListenToPgNotifyParallelAsync(ct), ct);

                // Start periodic poll fallback to ensure nothing missed
                //var pollTask = Task.Run(() => PollFallbackLoopParallelAsync(ct), ct);

                await Task.WhenAll(workers.Concat(new[] { listenerTask }));
            }
            else
            {
                if (_options.Mode == ElasticSyncMode.Realtime)
                {
                    await ListenToPgNotifyAsync(ct);
                }
                else if (_options.Mode == ElasticSyncMode.Interval)
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), ct);
                        await _elasticSyncNetService.ProcessChangeLogsAsync("0", _options.BatchSize, ct);
                    }
                }
            } 
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
    private async Task WorkerLoopParallelAsync(string workerId, int batchSize, CancellationToken cancellationToken)
    {
        // each worker has its own DB connection
        await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
        await conn.OpenAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            // Wait for a notification token
            try
            {
                await _notifyChannel.Reader.ReadAsync(cancellationToken);
            }
            catch (OperationCanceledException) { break; }

            // Process until no more batches (to drain quickly)
            while (!cancellationToken.IsCancellationRequested)
            {
                var hasWork = await _elasticSyncNetService.ProcessChangeLogsAsync(workerId, batchSize, cancellationToken);
                if (!hasWork)
                {
                    // No more logs to process, break out of the inner loop
                    break;
                }
            }
        }
    }

    private async Task ListenToPgNotifyParallelAsync(CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
        await conn.OpenAsync(ct);

        conn.Notification += (o, e) =>
        {
            for (int i = 0; i < _options.WorkerOptions.NumberOfWorkers; i++)
            {
                _ = _notifyChannel.Writer.WriteAsync(1);
            }

            Console.WriteLine($"[Listener] Notification received. Woke {_options.WorkerOptions.NumberOfWorkers} workers.");
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
                Console.WriteLine($"{Count++} -- Change detected, processing change logs...");
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

    private async Task PollFallbackLoopParallelAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // if channel is very empty, add a token so workers will try
            if (_notifyChannel.Reader.Count == 0)
            {
                await _notifyChannel.Writer.WriteAsync(1, ct);
            }
            await Task.Delay(_options.WorkerOptions.NotifyListenerPollMs, ct);
        }
    }

    
    private async Task ListenToPgNotifyAsync(CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"LISTEN {_namingPrefix}change_log_channel;";
        await cmd.ExecuteNonQueryAsync();

        while (!ct.IsCancellationRequested)
        {
            await conn.WaitAsync(ct);
            Console.WriteLine($"{Count++} -- Change detected, processing change logs...");

            await _elasticSyncNetService.ProcessChangeLogsAsync(null, _options.BatchSize, ct);
        }
    } 
}