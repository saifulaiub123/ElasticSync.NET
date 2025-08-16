using ElasticSync.NET.Models;
using ElasticSync.NET.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;
using ElasticSync.NET.Interface;
using ElasticSync.NET.Services;

namespace ElasticSync.NET.Extensions;

public static class ElasticSyncExtensions
{
    public static IServiceCollection AddElasticSyncEngine(
        this IServiceCollection services, 
        Action<ElasticSyncOptions> configure,
        Action<ElasticSyncOptions, IServiceCollection>? configureDatabaseServices = null)
    {
		try
		{
            var options = new ElasticSyncOptions();
            configure(options);

            configureDatabaseServices?.Invoke(options, services);

            services.AddSingleton(options);
            services.AddSingleton<ElasticClient>(_ =>
            {
                var settings = new ConnectionSettings(new Uri(options.ElasticsearchUrl));
                return new ElasticClient(settings);
            });

            services.AddSingleton<ElasticIndexProvisioner>();
            services.AddHostedService<StartupService>();
        }
		catch (Exception ex)
		{
            Console.WriteLine(ex.ToString());
		}
        return services;
    }
}