using ElasticSync.NET.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElasticSync.NET.Services
{
    public class StartupService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public StartupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken ct)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);

                    using var scope = _serviceProvider.CreateScope();
                    var installer = scope.ServiceProvider.GetService<IInstallerService>();

                    if (installer != null)
                        await installer.InstallAsync(ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error installing: {ex.Message}");
                }
            }, ct);

            return Task.CompletedTask; // Do not await the inner task — return immediately
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
