using ChangeSync.Elastic.Postgres.Models;
using ChangeSync.Elastic.Postgres.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;
using System.Threading.Tasks;

namespace ChangeSync.Elastic.Postgres.Extensions;

public static class ElasticSyncExtensions
{
    public static IServiceCollection AddElasticSyncEngine(this IServiceCollection services, Action<ChangeSyncOptions> setup)
    {
		try
		{
            var options = new ChangeSyncOptions();
            setup(options);

            services.AddSingleton(options);
            services.AddSingleton<ElasticClient>(_ =>
            {
                var settings = new ConnectionSettings(new Uri(options.ElasticsearchUrl));
                return new ElasticClient(settings);
            });

            services.AddSingleton<ChangeLogInstaller>();
            services.AddSingleton<ElasticIndexProvisioner>();
            services.AddHostedService<ChangeLogListenerService>();

            Task.Run(async () =>
            {
                var installer = new ChangeLogInstaller(options);
                await installer.InstallAsync();

                var client = new ElasticClient(new ConnectionSettings(new Uri(options.ElasticsearchUrl)));
                var provisioner = new ElasticIndexProvisioner(client, options);
                await provisioner.EnsureIndicesExistAsync();
            }).Wait();
        }
		catch (Exception ex)
		{
            Console.WriteLine(ex.ToString());

			var error =  ex;
		}

        return services;
    }
}