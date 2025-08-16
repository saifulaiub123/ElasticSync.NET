using ElasticSync.Models;
using ElasticSync.Net.PostgreSql.Services;
using ElasticSync.NET.Enum;
using ElasticSync.NET.Interface;
using ElasticSync.Services;
using Microsoft.Extensions.DependencyInjection;


namespace ElasticSync.Net.PostgreSql.Extentions
{

    public static class PostgreSqlElasticSyncExtensions
    {
        public static void AddElasticSyncPostgreSqlServices(
            this ElasticSyncOptions options,
            IServiceCollection services,
            string connectionString)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (services == null) throw new ArgumentNullException(nameof(services));

            // Configure internal options
            ((IDatabaseConfigurationHandler)options)
                .SetDatabaseProvider(DatabaseProvider.PostgreSQL, connectionString);

            // Register PostgreSQL-specific services
            services.AddSingleton<IChangeLogService, PostgreChangeLogService>();
            services.AddSingleton<IInstallerService, PostgreInstallerService>();

            services.AddHostedService<SyncListenerService>();

            // Future: add more PostgreSQL-specific DI registrations here
        }
    }
}

