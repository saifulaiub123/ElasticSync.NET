using ElasticSync.NET.Interface;
using ElasticSync.NET.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PgOutput2Json;
using System.Threading.Channels;

namespace ElasticSync.Net.PostgreSql.CDC.Services
{
    public class SyncListenerService : BackgroundService, ISyncListenerHostedService
    {
        private readonly ElasticSyncOptions _options;
        private readonly Channel<byte> _notifyChannel;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IChangeLogService _elasticSyncNetService;

        private readonly string _namingPrefix = "elastic_sync_";
        private int Count { get; set; } = 0;

        public SyncListenerService(ElasticSyncOptions options, IChangeLogService elasticSyncNetService, ILoggerFactory loggerFactory)
        {
            _options = options;
            _elasticSyncNetService = elasticSyncNetService;
            _loggerFactory = loggerFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            try
            {
                using var pgOutput2Json = PgOutput2JsonBuilder.Create()
                .WithLoggerFactory(_loggerFactory)
                .WithPgConnectionString("User ID=elastic_sync_rep_usr;Password=Pass@123;Host=localhost;Port=5432;Database=ElasticSyncTest;Connection Lifetime=0;")
                .WithPgPublications($"{_namingPrefix}pub")
                .WithPgReplicationSlot($"{_namingPrefix}slot")
                .WithMessageHandler((json, table, key, partition) =>
                {
                    Console.WriteLine($"{table}: {json}");
                    return Task.FromResult(true);
                })
                .Build();

                await pgOutput2Json.StartAsync(ct);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
