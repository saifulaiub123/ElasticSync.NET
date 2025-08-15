using ElasticSync.Models;
using ElasticSync.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;
using ElasticSync.NET.Interface;
using ElasticSync.NET.Models;
using ElasticSync.NET.Services;

namespace ElasticSync.Extensions;

public static class ElasticSyncExtensions
{
    public static IServiceCollection AddElasticSyncEngine(
        this IServiceCollection services, 
        Action<ElasticSyncOptions> configure,
        Action<ElasticSyncServiceProviders> providers)
    {
		try
		{
            var options = new ElasticSyncOptions();
            configure(options);

            var providerOptions = new ElasticSyncServiceProviders();
            providers(providerOptions);

            services.AddSingleton(options);
            services.AddSingleton<ElasticClient>(_ =>
            {
                var settings = new ConnectionSettings(new Uri(options.ElasticsearchUrl));
                return new ElasticClient(settings);
            });

            services.AddSingleton<ElasticIndexProvisioner>();

            if(providerOptions.ChangeLogServiceType != null)
                services.AddSingleton(typeof(IChangeLogService), providerOptions.ChangeLogServiceType);

            if (providerOptions.ChangeLogInstallerServiceType != null)
                services.AddSingleton(typeof(IInstallerService), providerOptions.ChangeLogInstallerServiceType);


            services.AddHostedService<SyncListenerService>();
            services.AddHostedService<StartupService>();
        }
		catch (Exception ex)
		{
            Console.WriteLine(ex.ToString());
		}
        return services;
    }
}