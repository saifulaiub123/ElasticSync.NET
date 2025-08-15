using ElasticSync.Models;
using ElasticSync.NET.Interface;
using ElasticSync.Services;
using Microsoft.Extensions.Hosting;
using Nest;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElasticSync.NET.Services
{
    public class StartupInstallerService : IHostedService
    {
        private readonly IInstallerService _installer;
        private readonly ElasticClient _client;
        private readonly ElasticSyncOptions _options;

        public StartupInstallerService(IInstallerService installer,
                                       ElasticClient client,
                                       ElasticSyncOptions options)
        {
            _installer = installer;
            _client = client;
            _options = options;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Optional delay to wait for DB or other dependencies
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            // Install database structures
            await _installer.InstallAsync();

            // Ensure Elasticsearch indices exist
            var provisioner = new ElasticIndexProvisioner(_client, _options);
            await provisioner.EnsureIndicesExistAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
