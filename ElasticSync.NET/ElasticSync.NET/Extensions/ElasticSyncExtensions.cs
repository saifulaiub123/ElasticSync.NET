using ElasticSync.Models;
using ElasticSync.Services;
//using ElasticSync.NET.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;
using System.Threading.Tasks;
using ElasticSync.NET.Interface;

namespace ElasticSync.Extensions;

public static class ElasticSyncExtensions
{
    public static IServiceCollection AddElasticSyncEngine<TRepo>(
        this IServiceCollection services, 
        Action<ElasticSyncOptions> configure)
        where TRepo : class, IChangeLogService
    {
		try
		{
            var options = new ElasticSyncOptions();
            configure(options);

            services.AddSingleton(options);
            services.AddSingleton<ElasticClient>(_ =>
            {
                var settings = new ConnectionSettings(new Uri(options.ElasticsearchUrl));
                return new ElasticClient(settings);
            });

            services.AddSingleton<ChangeLogInstaller>();
            services.AddSingleton<ElasticIndexProvisioner>();
            services.AddSingleton<IChangeLogService, TRepo>();
            services.AddHostedService<SyncListenerService>();

            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));

                var installer = new ChangeLogInstaller(options);
                await installer.InstallAsync();

                var client = new ElasticClient(new ConnectionSettings(new Uri(options.ElasticsearchUrl)));
                var provisioner = new ElasticIndexProvisioner(client, options);
                await provisioner.EnsureIndicesExistAsync();
            });
        }
		catch (Exception ex)
		{
            Console.WriteLine(ex.ToString());
		}
        return services;
    }
}