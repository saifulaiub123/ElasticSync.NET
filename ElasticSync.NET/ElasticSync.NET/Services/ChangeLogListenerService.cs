using Npgsql;
using Nest;
using ChangeSync.Elastic.Postgres.Models;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System;
using ElasticSync.NET.Services;

namespace ChangeSync.Elastic.Postgres.Services;

public class ChangeLogListenerService : BackgroundService
{
    private readonly ElasticClient _elastic;
    private readonly ChangeSyncOptions _options;
    private readonly IElasticSyncNetService _elasticSyncNetService;

    private readonly string _namingPrefix = "elastic_sync_";

    public ChangeLogListenerService(ElasticClient elastic, ChangeSyncOptions options, IElasticSyncNetService elasticSyncNetService)
    {
        _elastic = elastic;
        _options = options;
        _elasticSyncNetService = elasticSyncNetService;
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
                    await _elasticSyncNetService.ProcessChangeLogsAsync();
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
            await _elasticSyncNetService.ProcessChangeLogsAsync();
        }
    } 
}