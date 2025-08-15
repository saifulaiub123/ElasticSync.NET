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

namespace ChangeSync.Elastic.Postgres.Services;

public class SyncListenerService : BackgroundService
{
    private readonly ChangeSyncOptions _options;
    private readonly Channel<byte> _notifyChannel;
    private readonly IElasticSyncNetService _elasticSyncNetService;

    private readonly string _namingPrefix = "elastic_sync_";
    private int Count { get; set; } = 0;

    public SyncListenerService(ChangeSyncOptions options, IElasticSyncNetService elasticSyncNetService)
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
            var noOfWorkers = _options.EnableMultipleWorker ? _options.WorkerOptions.NumberOfWorkers : 1;
            var batchSize = _options.EnableMultipleWorker ? _options.WorkerOptions.BatchSizePerWorker : _options.BatchSize;

            var workers = Enumerable.Range(0, noOfWorkers)
                .Select(i =>
                    Task.Run(() =>
                        WorkerLoopParallelAsync(string.Format("worker_{0}", i + 1), batchSize, ct), ct)
                    )
                .ToList();

            if (_options.Mode == ElasticSyncMode.Realtime)
            {
                var listenerTask = Task.Run(() => ListenToPgNotifyParallelAsync(ct), ct);
                await Task.WhenAll(workers.Concat(new[] { listenerTask })); 
            }
            else if (_options.Mode == ElasticSyncMode.Interval)
            {
                var intervalFallbackListner = Task.Run(() => IntervalFallbackListener(ct), ct);
                await Task.WhenAll(workers.Concat(new[] { intervalFallbackListner }));
                
                //while (!ct.IsCancellationRequested)
                //{
                //    await Task.Delay(TimeSpan.FromSeconds(_options.IntervalInSeconds), ct);

                //    if (_notifyChannel.Reader.Count == 0)
                //    {
                //        await _notifyChannel.Writer.WriteAsync(1, ct);
                //    }
                //}
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
    private async Task WorkerLoopParallelAsync(string workerId, int batchSize, CancellationToken ct)
    {
        // each worker has its own DB connection
        await using var conn = new NpgsqlConnection(_options.PostgresConnectionString);
        await conn.OpenAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            // Wait for a notification token
            try
            {
                await _notifyChannel.Reader.ReadAsync(ct);
            }
            catch (OperationCanceledException) { break; }

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
            // if channel is very empty, add a token so workers will try
            //if (_notifyChannel.Reader.Count == 0)
            //{
                
            //}
            await Task.Delay(TimeSpan.FromSeconds(_options.IntervalInSeconds), ct);
            await _notifyChannel.Writer.WriteAsync(1, ct);
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

            var hasWork = await _elasticSyncNetService.ProcessChangeLogsAsync(null, _options.BatchSize, ct);
            if (!hasWork)
            {
                // No more logs to process, break out of the inner loop
                break;
            }
        }
    } 
}